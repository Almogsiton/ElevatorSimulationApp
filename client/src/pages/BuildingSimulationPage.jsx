import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { buildingService, elevatorCallService } from '../services/apiService';
import { useAuth } from '../contexts/AuthContext';
import ElevatorStatus from '../components/ElevatorStatus';
import CallsDetails from '../components/CallsDetails';
import { ELEVATOR_SIMULATION_CONFIG } from '../config/config';
import '../styles/BuildingSimulation.css';

// Main elevator simulation page - displays building floors, elevator status, and handles real-time updates

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
  const [ setShowDestinationButtons] = useState(false);
  const [calls, setCalls] = useState([]); 
  const [toast, setToast] = useState(null); 
  const [pendingCalls, setPendingCalls] = useState(() => {
    const saved = localStorage.getItem('pendingCalls_' + buildingId);
    return saved ? JSON.parse(saved) : [];
  });
  const connectionRef = useRef(null);


  useEffect(() => {
    loadBuilding();
    loadCalls();
    return () => {
      if (connectionRef.current) {
        connectionRef.current.stop();
      }
    };
  }, [buildingId]);

  useEffect(() => {
    if (elevator?.id) {
      setupSignalR();
    }
  }, [elevator?.id]);

  useEffect(() => {
    if (elevator && calls.length > 0) {
      const currentFloor = elevator.currentFloor;
      const floorCall = floorCalls.find(c => c.requestedFloor === currentFloor);
      if (floorCall) {
        showToast(`Elevator arrived at floor ${currentFloor + 1} (call)`);
      }
    }
    // eslint-disable-next-line
  }, [elevator?.currentFloor]);

  const showToast = (msg) => {
    setToast(msg);
    setTimeout(() => setToast(null), ELEVATOR_SIMULATION_CONFIG.TOAST_DURATION);
  };

  const loadCalls = async () => {
    try {
      const allCalls = await elevatorCallService.getBuildingCalls(numericBuildingId);
      setCalls(allCalls.filter(c => !c.isHandled));
    } catch (err) {
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
        .withUrl(ELEVATOR_SIMULATION_CONFIG.SIGNALR_URL)
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

  const getPendingCallPriority = (call, elevator, building) => {
    if (!elevator || elevator.direction === 'None' || elevator.direction === 2) return 0;
    const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
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
    if (pendingCalls.some(c => c.floor === floor && c.direction === direction)) {
              showToast('A pending call already exists for this floor and direction');
      return;
    }
    if (floorCalls.some(c => c.requestedFloor === floor && c.direction === direction)) {
              showToast('An active call already exists for this floor and direction');
      return;
    }
    if (!isCallOnTheWay(floor, direction)) {
      setPendingCalls(prev => {
        const newCall = { floor, direction, time: Date.now() };
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

  const handleFloorNumberClick = async (floor) => {
    if (elevator.currentFloor === floor) return;
    
    if (isCallOnTheWay(floor, 'up') || isCallOnTheWay(floor, 'down')) {
      try {
        const call = await elevatorCallService.createCall(numericBuildingId, floor);
        setError('');
        loadCalls();
        showToast(`Call to floor ${floor} sent`);
      } catch (error) {
        setError(error.message);
      }
    } else {
      if (pendingCalls.some(c => c.floor === floor)) {
        showToast('A pending call already exists for this floor');
        return;
      }
      
      setPendingCalls(prev => {
        let direction = 'up';
        if (floor < elevator.currentFloor) {
          direction = 'down';
        } else if (floor > elevator.currentFloor) {
          direction = 'up';
        } else {
          direction = floor === 0 ? 'up' : 'down';
        }
        
        const newCall = { floor, direction, time: Date.now() };
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
      showToast(`Call to floor ${floor} saved to pending calls`);
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

  const getElevatorPosition = () => {
    if (!elevator || !building) return 0;
    const floorHeight = 400 / building.numberOfFloors;
    return elevator.currentFloor * floorHeight;
  };

  const floorCalls = calls.filter(c => c.destinationFloor === null);

  const upCalls = floorCalls.filter(c => c.direction === 'up');
  const downCalls = floorCalls.filter(c => c.direction === 'down');



  const isCallOnTheWay = (floor, direction) => {
    if (!elevator) return true;
    if (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2) return true;
    const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
    
    // עולה
    if (elevator.direction === 'Up' || elevator.direction === 0) {
      if (floor === maxFloor && direction === 'down') return true;
      if (direction === 'up' && floor > elevator.currentFloor) return true;
      return false;
    }
    
    if (elevator.direction === 'Down' || elevator.direction === 1) {
      if (floor === 0 && direction === 'up') return true;
      if (direction === 'down' && floor < elevator.currentFloor) return true;
      return false;
    }
    
    return false;
  };

  const getFloorTop = (floor) => {
    const totalFloors = building.numberOfFloors;
    const shaftHeight = ELEVATOR_SIMULATION_CONFIG.FLOOR_HEIGHT * totalFloors;
    return (totalFloors - 1 - floor) * ELEVATOR_SIMULATION_CONFIG.FLOOR_HEIGHT + ELEVATOR_SIMULATION_CONFIG.SHAFT_PADDING;
  };

  useEffect(() => {
    localStorage.setItem('pendingCalls_' + buildingId, JSON.stringify(pendingCalls));
  }, [pendingCalls, buildingId]);

  useEffect(() => {
    if (!elevator || pendingCalls.length === 0) return;
    const isElevatorFree = (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2);
    if (isElevatorFree && floorCalls.length === 0 && pendingCalls.length > 0) {
      const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
      
      const allUp = pendingCalls.every(call => call.direction === 'up');
      const allDown = pendingCalls.every(call => call.direction === 'down');
      
      let newDirection = null;
      
      if (allUp) {
        newDirection = 'up';
      } else if (allDown) {
        newDirection = 'down';
      } else {
        const downFromTop = pendingCalls.some(call => call.direction === 'down' && call.floor === maxFloor);
        const upFromBottom = pendingCalls.some(call => call.direction === 'up' && call.floor === 0);
        
        if (downFromTop && elevator.currentFloor === maxFloor) {
          newDirection = 'down';
        } else if (upFromBottom && elevator.currentFloor === 0) {
          newDirection = 'up';
        } else {
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
            newDirection = closestCall.direction;
          }
        }
      }
      
      const callsToSend = [];
      const callsToKeep = [];
      
      pendingCalls.forEach(call => {
        let shouldSend = false;
        
        if (newDirection === 'up') {
          if (call.direction === 'up' && call.floor > elevator.currentFloor) {
            shouldSend = true;
          } else if (call.direction === 'down' && call.floor === maxFloor) {
            shouldSend = true;
          }
        } else if (newDirection === 'down') {
          if (call.direction === 'down' && call.floor < elevator.currentFloor) {
            shouldSend = true;
          } else if (call.direction === 'up' && call.floor === 0) {
            shouldSend = true;
          }
        }
        
        if (shouldSend) {
          callsToSend.push(call);
        } else {
          callsToKeep.push(call);
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
  }, [elevator?.status, elevator?.direction, floorCalls, pendingCalls]);

  const prevDirectionRef = useRef();
  useEffect(() => {
    if (!elevator) return;
    if (prevDirectionRef.current && elevator.direction !== prevDirectionRef.current && pendingCalls.length > 0) {
      const newDirection = elevator.direction === 0 || elevator.direction === 'Up' ? 'up'
        : elevator.direction === 1 || elevator.direction === 'Down' ? 'down'
        : null;
      if (!newDirection) return;
      const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
      const callsToSend = [];
      const callsToKeep = [];
      pendingCalls.forEach(call => {
        let shouldSend = false;
        
        if (newDirection === 'up') {
          if (call.direction === 'up' && call.floor > elevator.currentFloor) {
            shouldSend = true;
          } else if (call.direction === 'down' && call.floor === maxFloor) {
            shouldSend = true;
          }
        } else if (newDirection === 'down') {
          if (call.direction === 'down' && call.floor < elevator.currentFloor) {
            shouldSend = true;
          } else if (call.direction === 'up' && call.floor === 0) {
            shouldSend = true;
          }
        }
        
        if (shouldSend) {
          callsToSend.push(call);
        } else {
          callsToKeep.push(call);
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

  const sortedFloorCalls = [...floorCalls].sort((a, b) => a.requestedFloor - b.requestedFloor);
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
        <div className="toast-notification">
          {toast}
        </div>
      )}
      <div className="card">
        <button className="btn btn-secondary back-button" onClick={() => navigate('/buildings')}>
          Back to Buildings
        </button>
        <h2>{building.name}</h2>
        <div className="elevator-status-bar">
          <ElevatorStatus elevator={elevator} />
        </div>

        <div className="building-shaft-container">
          <div className="building-shaft" style={{ height: ELEVATOR_SIMULATION_CONFIG.FLOOR_HEIGHT * building.numberOfFloors }}>
            {Array.from({ length: building.numberOfFloors }, (_, i) => building.numberOfFloors - 1 - i).map((floor) => (
              <div
                key={floor}
                className={`building-floor-row${elevator.currentFloor === floor ? ' current-floor-row' : ''}`}
              >
                <div className="floor-buttons-row" style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                  {/* Floor number button (simulates elevator panel) */}
                  <button
                    className="floor-btn floor-number-btn"
                    onClick={() => handleFloorNumberClick(floor)}
                    disabled={elevator.currentFloor === floor}
                    title={elevator.currentFloor === floor ? 'You are here' : 'Go to floor ' + floor}
                  >
                    {floor}
                  </button>
                  {/* Up button */}
                  {floor !== building.numberOfFloors - 1 && (
                    <button
                      className="floor-btn up"
                      onClick={() => handleCallElevator(floor, 'up')}
                      disabled={elevator.currentFloor === floor}
                      title="Call elevator up"
                    >▲</button>
                  )}
                  {/* Down button */}
                  {floor !== 0 && (
                    <button
                      className="floor-btn down"
                      onClick={() => handleCallElevator(floor, 'down')}
                      disabled={elevator.currentFloor === floor}
                      title="Call elevator down"
                    >▼</button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
        {/* Requests lists */}
        <CallsDetails 
          sortedFloorCalls={sortedFloorCalls}
          sortedPendingCalls={sortedPendingCalls}
        />
      </div>
    </div>
  );
};

export default BuildingSimulationPage; 