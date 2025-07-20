import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { buildingService, elevatorCallService } from '../services/apiService';
import { useAuth } from '../contexts/AuthContext';
import { API_CONFIG } from '../config/config.js';
import ElevatorMetadata from '../components/ElevatorMetadata';
import ElevatorShaft from '../components/ElevatorShaft';
import CallsDetails from '../components/CallsDetails';

const BuildingSimulationPage = () => {
  const { buildingId } = useParams();
  const numericBuildingId = Number(buildingId);
  const navigate = useNavigate();
  const { user } = useAuth();
  const [building, setBuilding] = useState(null);
  const [elevator, setElevator] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeCall, setActiveCall] = useState(null);
  const [setShowDestinationButtons] = useState(false);
  const [calls, setCalls] = useState([]); // All active calls
  const [toast, setToast] = useState(null); // Pop-up notification
  const [pendingCalls, setPendingCalls] = useState(() => {
    // Load from localStorage if exists
    const saved = localStorage.getItem('pendingCalls_' + buildingId);
    return saved ? JSON.parse(saved) : [];
  });
  const connectionRef = useRef(null);

  // load data
  useEffect(() => {
    loadBuilding();
    loadCalls();
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [buildingId]);

  // setup singleR
  useEffect(() => {
    if (elevator?.id) {
      setupSignalR();
    }
  }, [elevator?.id]);

  // show toast when elevator arrives at floor
  useEffect(() => {
    if (elevator && calls.length > 0) {
      // Check if elevator arrived at floor with active call or destination
      const currentFloor = elevator.currentFloor;
      const floorCall = floorCalls.find(c => c.requestedFloor === currentFloor);
      const elevatorRequest = elevatorRequests.find(c => c.destinationFloor === currentFloor);
      if (floorCall) {
        showToast(`Elevator arrived at floor ${currentFloor + 1} (call)`);
      } else if (elevatorRequest) {
        showToast(`Elevator arrived at floor ${currentFloor + 1} (destination)`);
      }
    }
    // eslint-disable-next-line
  }, [elevator?.currentFloor]);

  const showToast = (msg) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2500);
  };

  // Load all active calls
  const loadCalls = async () => {
    try {
      const allCalls = await elevatorCallService.getBuildingCalls(numericBuildingId);
      setCalls(allCalls.filter(c => !c.isHandled));
    } catch (err) {
      // Don't show error, just clear
      setCalls([]);
    }
  };

  const loadBuilding = async () => {
    try {
      const data = await buildingService.getBuilding(numericBuildingId, user.userId);
      setBuilding(data);
      setElevator(data.elevator);
    } catch (error) {
      setError(error.message);
    } finally {
      setLoading(false);
    }
  };

  const setupSignalR = async () => {
    if (!elevator?.id) {
      console.log('No elevator ID available for SignalR connection');
      return;
    }
    try {
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(API_CONFIG.SIGNALR_HUB_URL)
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveElevatorUpdate', (message) => {
        setElevator(prev => ({
          ...prev,
          currentFloor: message.currentFloor,
          status: message.status,
          direction: message.direction,
          doorStatus: message.doorStatus
        }));
        // Reload calls on every update
        loadCalls();
        if (message.doorStatus === 'Open' && activeCall) {
          setShowDestinationButtons(true);
        }
      });

      await connection.start();
      await connection.invoke('JoinElevatorGroup', elevator.id);
      connectionRef.current = connection;
    } catch (error) {
      console.error('SignalR connection failed:', error);
    }
  };

  // Priority function for pending call
  const getPendingCallPriority = (call, elevator, building) => {
    if (!elevator || elevator.direction === 'None' || elevator.direction === 2) return 0;
    const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
    
    // Handle floorNumber calls
    if (call.type === 'floorNumber') {
      if (elevator.direction === 'up' || elevator.direction === 0) {
        if (call.floor < elevator.currentFloor) return 1; // Going down from higher floor
        return 2; // Going up
      }
      if (elevator.direction === 'down' || elevator.direction === 1) {
        if (call.floor > elevator.currentFloor) return 1; // Going up from lower floor
        return 2; // Going down
      }
      return 0;
    }
    
    // Handle regular direction-based calls
    if (elevator.direction === 'up' || elevator.direction === 0) {
      if (call.direction === 'down') return 1;
      if (call.floor === 0 && call.direction === 'up') return 1;
      return 2;
    }
    if (elevator.direction === 'down' || elevator.direction === 1) {
      if (call.direction === 'up') return 1;
      if (call.floor === maxFloor && call.direction === 'down') return 1;
      return 2;
    }
    return 0;
  };

  const handleCallElevator = async (floor, direction) => {
    // Prevent duplicates in pendingCalls
    if (pendingCalls.some(c => c.floor === floor && c.direction === direction)) {
      showToast('Call already pending for this floor and direction');
      return;
    }
    // Prevent duplicates in active calls
    if (floorCalls.some(c => c.requestedFloor === floor && c.direction === direction)) {
      showToast('Active call already exists for this floor and direction');
      return;
    }
    if (!isCallOnTheWay(floor, direction)) {
      // Smart insertion in the right place
      setPendingCalls(prev => {
        const newCall = { floor, direction, time: Date.now() };
        // Prevent duplicates (extra safety)
        if (prev.some(c => c.floor === floor && c.direction === direction)) return prev;
        const idx = prev.findIndex(c => {
          const pNew = getPendingCallPriority(newCall, elevator, building);
          const pC = getPendingCallPriority(c, elevator, building);
          if (pNew < pC) return true;
          if (pNew === pC && newCall.time < c.time) return true;
          return false;
        });
        if (idx === -1) return [...prev, newCall];
        return [...prev.slice(0, idx), newCall, ...prev.slice(idx)];
      });
      showToast('Call saved to pending calls and will be sent when elevator changes direction');
      return;
    }
    try {
      const call = await elevatorCallService.createCall(numericBuildingId, floor);
      setActiveCall(call);
      setError('');
      loadCalls();
    } catch (error) {
      setError(error.message);
    }
  };

  const handleSelectDestination = async (destinationFloor) => {
    if (!activeCall) return;
    try {
      await elevatorCallService.updateCall(activeCall.id, destinationFloor);
      setActiveCall(null);
      setShowDestinationButtons(false);
      setError('');
      loadCalls();
    } catch (error) {
      setError(error.message);
    }
  };

  // New function to handle floor number button clicks
  const handleFloorNumberCall = async (floor) => {
    // Prevent duplicates in pendingCalls
    if (pendingCalls.some(c => c.floor === floor && c.type === 'floorNumber')) {
      showToast('Call already pending for this floor');
      return;
    }
    // Prevent duplicates in active calls
    if (floorCalls.some(c => c.requestedFloor === floor)) {
      showToast('Active call already exists for this floor');
      return;
    }

    // Check if the floor is "on the way" for the elevator
    const isOnTheWay = isFloorOnTheWay(floor);
    
    if (isOnTheWay) {
      // Add to Floor Calls (immediate call)
      try {
        const call = await elevatorCallService.createCall(numericBuildingId, floor);
        setError('');
        loadCalls();
        showToast(`Call created for floor ${floor} (on the way)`);
      } catch (error) {
        setError(error.message);
      }
    } else {
      // Add to Pending Calls
      setPendingCalls(prev => {
        const newCall = { 
          floor, 
          type: 'floorNumber', 
          time: Date.now(),
          direction: floor > elevator.currentFloor ? 'up' : 'down'
        };
        // Prevent duplicates (extra safety)
        if (prev.some(c => c.floor === floor && c.type === 'floorNumber')) return prev;
        const idx = prev.findIndex(c => {
          const pNew = getPendingCallPriority(newCall, elevator, building);
          const pC = getPendingCallPriority(c, elevator, building);
          if (pNew < pC) return true;
          if (pNew === pC && newCall.time < c.time) return true;
          return false;
        });
        if (idx === -1) return [...prev, newCall];
        return [...prev.slice(0, idx), newCall, ...prev.slice(idx)];
      });
      showToast(`Call saved to pending calls for floor ${floor} (not on the way)`);
    }
  };

  // Helper: Is the floor "on the way" for the elevator?
  const isFloorOnTheWay = (floor) => {
    if (!elevator) return true;
    if (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2) return true;
    
    // Going up
    if (elevator.direction === 'Up' || elevator.direction === 0) {
      return floor >= elevator.currentFloor;
    }
    
    // Going down
    if (elevator.direction === 'Down' || elevator.direction === 1) {
      return floor <= elevator.currentFloor;
    }
    
    return false;
  };

  // Call filters
  const floorCalls = calls.filter(c => c.destinationFloor === null);
  const elevatorRequests = calls.filter(c => c.destinationFloor !== null);

  // Helper: Is the call "on the way"?
  const isCallOnTheWay = (floor, direction) => {
    if (!elevator) return true;
    if (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2) return true;
    const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
    
    // Going up
    if (elevator.direction === 'Up' || elevator.direction === 0) {
      // Call to go down from top floor
      if (floor === maxFloor && direction === 'down') return true;
      // Call to go up from floor higher than elevator
      if (direction === 'up' && floor > elevator.currentFloor) return true;
      return false;
    }
    
    // Going down
    if (elevator.direction === 'Down' || elevator.direction === 1) {
      // Call to go up from bottom floor
      if (floor === 0 && direction === 'up') return true;
      // Call to go down from floor lower than elevator
      if (direction === 'down' && floor < elevator.currentFloor) return true;
      return false;
    }
    
    return false;
  };

  // Save to localStorage on every change
  useEffect(() => {
    localStorage.setItem('pendingCalls_' + buildingId, JSON.stringify(pendingCalls));
  }, [pendingCalls, buildingId]);

  // Send relevant pending calls when elevator is free and no active calls
  useEffect(() => {
    if (!elevator || pendingCalls.length === 0) return;
    const isElevatorFree = (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2);
    if (isElevatorFree && floorCalls.length === 0 && elevatorRequests.length === 0 && pendingCalls.length > 0) {
      const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
      
      // Check if all pending calls are in the same direction
      const allUp = pendingCalls.every(call => 
        call.direction === 'up' || (call.type === 'floorNumber' && call.floor > elevator.currentFloor)
      );
      const allDown = pendingCalls.every(call => 
        call.direction === 'down' || (call.type === 'floorNumber' && call.floor < elevator.currentFloor)
      );
      
      let newDirection = null;
      
      if (allUp) {
        newDirection = 'up';
      } else if (allDown) {
        newDirection = 'down';
      } else {
        // Mixed directions - need smarter logic
        // First, check if there are calls to go down from top floor
        const downFromTop = pendingCalls.some(call => 
          (call.direction === 'down' && call.floor === maxFloor) || 
          (call.type === 'floorNumber' && call.floor === maxFloor)
        );
        // Check if there are calls to go up from bottom floor
        const upFromBottom = pendingCalls.some(call => 
          (call.direction === 'up' && call.floor === 0) || 
          (call.type === 'floorNumber' && call.floor === 0)
        );
        
        if (downFromTop && elevator.currentFloor === maxFloor) {
          // Elevator at top floor and there's a call to go down - go down
          newDirection = 'down';
        } else if (upFromBottom && elevator.currentFloor === 0) {
          // Elevator at bottom floor and there's a call to go up - go up
          newDirection = 'up';
        } else {
          // Determine by closest call to elevator
          const closestCall = pendingCalls.reduce((closest, current) => {
            const closestDistance = Math.abs(closest.floor - elevator.currentFloor);
            const currentDistance = Math.abs(current.floor - elevator.currentFloor);
            return currentDistance < closestDistance ? current : closest;
          });
          
          if (closestCall.floor > elevator.currentFloor) {
            newDirection = 'up';
          } else if (closestCall.floor < elevator.currentFloor) {
            newDirection = 'down';
          } else {
            newDirection = closestCall.direction || (closestCall.type === 'floorNumber' ? 'up' : 'up');
          }
        }
      }
      
      // Release only calls that match the new direction
      const callsToSend = [];
      const callsToKeep = [];
      
      pendingCalls.forEach(call => {
        // Handle floorNumber calls
        if (call.type === 'floorNumber') {
          if (
            (newDirection === 'up' && call.floor > elevator.currentFloor) ||
            (newDirection === 'down' && call.floor < elevator.currentFloor) ||
            // Edge case: all calls in same direction, then release all
            (allUp && newDirection === 'up') ||
            (allDown && newDirection === 'down')
          ) {
            callsToSend.push(call);
          } else {
            callsToKeep.push(call);
          }
        } else {
          // Handle regular direction-based calls
          if (
            (newDirection === 'up' && call.direction === 'up' && call.floor > elevator.currentFloor) ||
            (newDirection === 'up' && call.direction === 'down' && call.floor === maxFloor) ||
            (newDirection === 'down' && call.direction === 'down' && call.floor < elevator.currentFloor) ||
            (newDirection === 'down' && call.direction === 'up' && call.floor === 0) ||
                      // Edge case: all calls in same direction, then release all
          (allUp && newDirection === 'up' && (call.direction === 'up' || call.type === 'floorNumber')) ||
          (allDown && newDirection === 'down' && (call.direction === 'down' || call.type === 'floorNumber'))
          ) {
            callsToSend.push(call);
          } else {
            callsToKeep.push(call);
          }
        }
      });
      
      if (callsToSend.length > 0) {
        callsToSend.forEach(async (call) => {
          await elevatorCallService.createCall(numericBuildingId, call.floor);
        });
        setPendingCalls(callsToKeep);
        showToast('Relevant pending calls sent to elevator');
        loadCalls();
      }
    }
    // eslint-disable-next-line
  }, [elevator?.status, elevator?.direction, floorCalls, elevatorRequests, pendingCalls]);

  // Send pending calls on direction change
  const prevDirectionRef = useRef();
  useEffect(() => {
    if (!elevator) return;
    if (prevDirectionRef.current && elevator.direction !== prevDirectionRef.current && pendingCalls.length > 0) {
      // Determine new direction
      const newDirection = elevator.direction === 0 || elevator.direction === 'Up' ? 'up'
        : elevator.direction === 1 || elevator.direction === 'Down' ? 'down'
        : null;
      if (!newDirection) return;
      const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
      const callsToSend = [];
      const callsToKeep = [];
      pendingCalls.forEach(call => {
        // Handle floorNumber calls
        if (call.type === 'floorNumber') {
          if (
            (newDirection === 'up' && call.floor > elevator.currentFloor) ||
            (newDirection === 'down' && call.floor < elevator.currentFloor)
          ) {
            callsToSend.push(call);
          } else {
            callsToKeep.push(call);
          }
        } else {
          // Handle regular direction-based calls
          if (
            (newDirection === 'up' && call.direction === 'up' && call.floor > elevator.currentFloor) ||
            (newDirection === 'up' && call.direction === 'down' && call.floor === maxFloor) ||
            (newDirection === 'down' && call.direction === 'down' && call.floor < elevator.currentFloor) ||
            (newDirection === 'down' && call.direction === 'up' && call.floor === 0)
          ) {
            callsToSend.push(call);
          } else {
            callsToKeep.push(call);
          }
        }
      });
      if (callsToSend.length > 0) {
        callsToSend.forEach(async (call) => {
          await elevatorCallService.createCall(numericBuildingId, call.floor);
        });
        setPendingCalls(callsToKeep);
        showToast('Relevant pending calls sent to elevator');
        loadCalls();
      }
    }
    prevDirectionRef.current = elevator.direction;
    // eslint-disable-next-line
  }, [elevator?.direction]);

  // Sort lists for display
  const sortedFloorCalls = [...floorCalls].sort((a, b) => a.requestedFloor - b.requestedFloor);
  // Smart sorting of pending calls
  const sortedPendingCalls = [...pendingCalls].sort((a, b) => {
    const pa = getPendingCallPriority(a, elevator, building);
    const pb = getPendingCallPriority(b, elevator, building);
    if (pa !== pb) return pa - pb;
    return a.time - b.time;
  });

  if (loading) {
    return (
      <div className="container">
        <div className="card">
          <p>Loading building...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="container">
        <div className="card">
          <div className="error">{error}</div>
          <button className="btn" onClick={() => navigate('/buildings')}>
            Back to Buildings
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="container">
      {toast && (
        <div style={{
          position: 'fixed', top: 30, right: 30, zIndex: 9999,
          background: '#667eea', color: 'white', padding: '16px 32px', borderRadius: 12,
          boxShadow: '0 4px 12px rgba(0,0,0,0.15)', fontSize: 18, fontWeight: 500
        }}>
          {toast}
        </div>
      )}
      <div className="card">
        <button className="btn btn-secondary" style={{ float: 'right', marginBottom: 12 }} onClick={() => navigate('/buildings')}>
          Back to Buildings
        </button>
        <h2>{building.name}</h2>
        
        {/* Compact Metadata Row */}
        <ElevatorMetadata 
          elevator={elevator}
          activeCall={activeCall}
          sortedFloorCalls={sortedFloorCalls}
          sortedPendingCalls={sortedPendingCalls}
        />

        {/* Active Call Info */}
        {activeCall && (
          <div className="card" style={{ marginBottom: '16px', padding: '12px 16px' }}>
            <h3 style={{ margin: '0 0 8px 0', fontSize: '16px' }}>Active Call</h3>
            <p style={{ margin: '0', fontSize: '14px' }}>Calling elevator to floor {activeCall.requestedFloor}</p>
            {elevator.doorStatus === 'Open' && elevator.currentFloor === activeCall.requestedFloor && (
              <p style={{ color: '#28a745', fontWeight: 'bold', margin: '8px 0 0 0', fontSize: '14px' }}>
                Elevator arrived! Select your destination floor.
              </p>
            )}
          </div>
        )}

        {/* Elevator Shaft */}
        <ElevatorShaft 
          building={building}
          elevator={elevator}
          handleCallElevator={handleCallElevator}
          handleSelectDestination={handleSelectDestination}
          handleFloorNumberCall={handleFloorNumberCall}
        />

        {/* Calls Details */}
        <CallsDetails 
          sortedFloorCalls={sortedFloorCalls}
          sortedPendingCalls={sortedPendingCalls}
        />
      </div>
    </div>
  );
};

export default BuildingSimulationPage; 