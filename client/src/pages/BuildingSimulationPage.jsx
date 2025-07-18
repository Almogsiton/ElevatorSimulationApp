import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { buildingService, elevatorCallService } from '../services/apiService';
import { useAuth } from '../contexts/AuthContext';
import ElevatorStatus from '../components/ElevatorStatus';
import FloorButton from '../components/FloorButton';

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
  const [showDestinationButtons, setShowDestinationButtons] = useState(false);
  const [calls, setCalls] = useState([]); // כל הבקשות הפעילות
  const [toast, setToast] = useState(null); // הודעת פופ-אפ
  const [pendingCalls, setPendingCalls] = useState(() => {
    // Load from localStorage if exists
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
      // בדוק אם המעלית הגיעה לקומה עם קריאה פעילה או יעד פעיל
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

  // שליפת כל הבקשות הפעילות
  const loadCalls = async () => {
    try {
      const allCalls = await elevatorCallService.getBuildingCalls(numericBuildingId);
      setCalls(allCalls.filter(c => !c.isHandled));
    } catch (err) {
      // לא נציג שגיאה, רק נרוקן
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
        .withUrl('http://localhost:5091/elevatorHub')
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
        // בכל עדכון, נטען מחדש את הבקשות
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

  const handleCallElevator = async (floor, direction) => {
    if (!isCallOnTheWay(floor, direction)) {
      // לא "על הדרך" – הוסף ל-pendingCalls
      setPendingCalls(prev => [...prev, { floor, direction, time: Date.now() }]);
      showToast('הקריאה נשמרה לקריאות ממתינות ותישלח כאשר המעלית תשנה כיוון');
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

  const getElevatorPosition = () => {
    if (!elevator || !building) return 0;
    const floorHeight = 400 / building.numberOfFloors;
    return elevator.currentFloor * floorHeight;
  };

  // פילטרים לבקשות
  const floorCalls = calls.filter(c => c.destinationFloor === null);
  const elevatorRequests = calls.filter(c => c.destinationFloor !== null);

  // Split floorCalls into up and down calls
  const upCalls = floorCalls.filter(c => c.direction === 'up');
  const downCalls = floorCalls.filter(c => c.direction === 'down');

  // הוסף פונקציה לבדיקת יעד פעיל
  const isDestinationActive = (floor) => {
    return elevatorRequests.some(c => c.destinationFloor + 1 === floor);
  };

  // Helper: האם הקריאה "על הדרך"?
  const isCallOnTheWay = (floor, direction) => {
    if (!elevator) return true; // אם אין מידע – נאפשר
    if (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2) return true;
    if (elevator.direction === 'Up' || elevator.direction === 0) {
      if (direction === 'up' && floor > elevator.currentFloor) {
        return true;
      } else {
        console.log(`לא על הדרך: קומה ${floor}, כיוון קריאה ${direction}, מצב מעלית עולה, קומה נוכחית ${elevator.currentFloor}, elevator.direction=${JSON.stringify(elevator.direction)}`);
        return false;
      }
    }
    if (elevator.direction === 'Down' || elevator.direction === 1) {
      if (direction === 'down' && floor < elevator.currentFloor) {
        return true;
      } else {
        console.log(`לא על הדרך: קומה ${floor}, כיוון קריאה ${direction}, מצב מעלית יורדת, קומה נוכחית ${elevator.currentFloor}, elevator.direction=${JSON.stringify(elevator.direction)}`);
        return false;
      }
    }
    console.log(`לא על הדרך: קומה ${floor}, כיוון קריאה ${direction}, מצב מעלית לא מזוהה, קומה נוכחית ${elevator.currentFloor}, elevator.direction=${JSON.stringify(elevator.direction)}`);
    return false;
  };

  // Helper to calculate elevator car position
  const getFloorTop = (floor) => {
    const totalFloors = building.numberOfFloors;
    const shaftHeight = 60 * totalFloors; // 60px per floor
    return (totalFloors - 1 - floor) * 60 + 8; // 8px padding
  };

  // שמירה ל-localStorage בכל שינוי
  useEffect(() => {
    localStorage.setItem('pendingCalls_' + buildingId, JSON.stringify(pendingCalls));
  }, [pendingCalls, buildingId]);

  // שליחת קריאות ממתינות רלוונטיות כאשר המעלית פנויה ואין קריאות פעילות
  useEffect(() => {
    if (!elevator || pendingCalls.length === 0) return;
    // אם המעלית פנויה (Idle/None) ואין קריאות פעילות כלל
    const isElevatorFree = (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2);
    if (isElevatorFree && floorCalls.length === 0 && elevatorRequests.length === 0) {
      const callsToSend = [];
      const callsToKeep = [];
      pendingCalls.forEach(call => {
        if (isCallOnTheWay(call.floor, call.direction)) {
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
        showToast('קריאות ממתינות רלוונטיות נשלחו למעלית');
        loadCalls();
      }
    }
    // eslint-disable-next-line
  }, [elevator?.status, elevator?.direction, floorCalls, elevatorRequests, pendingCalls]);

  // שליחת קריאות ממתינות בעת שינוי כיוון
  const prevDirectionRef = useRef();
  useEffect(() => {
    if (!elevator) return;
    if (prevDirectionRef.current && elevator.direction !== prevDirectionRef.current && pendingCalls.length > 0) {
      // כיוון השתנה – שלח את כל הקריאות הממתינות
      pendingCalls.forEach(async (call) => {
        await elevatorCallService.createCall(numericBuildingId, call.floor);
      });
      setPendingCalls([]);
      showToast('כל הקריאות הממתינות נשלחו למעלית');
      loadCalls();
    }
    prevDirectionRef.current = elevator.direction;
    // eslint-disable-next-line
  }, [elevator?.direction]);

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
        <div className="elevator-status-bar">
          <ElevatorStatus elevator={elevator} />
        </div>
        {activeCall && (
          <div className="card" style={{ marginTop: '20px' }}>
            <h3>Active Call</h3>
            <p>Calling elevator to floor {activeCall.requestedFloor}</p>
            {elevator.doorStatus === 'Open' && elevator.currentFloor === activeCall.requestedFloor && (
              <p style={{ color: '#28a745', fontWeight: 'bold' }}>
                Elevator arrived! Select your destination floor.
              </p>
            )}
          </div>
        )}
        <div className="building-shaft-container">
          <div className="building-shaft" style={{ height: 60 * building.numberOfFloors }}>
            {Array.from({ length: building.numberOfFloors }, (_, i) => building.numberOfFloors - 1 - i).map((floor) => (
              <div
                key={floor}
                className={`building-floor-row${elevator.currentFloor === floor ? ' current-floor-row' : ''}`}
              >
                <div className="floor-buttons-row" style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
                  {/* Floor number button (simulates elevator panel) */}
                  <button
                    className="floor-btn floor-number-btn"
                    onClick={() => handleSelectDestination(floor)}
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
        <div className="requests-lists" style={{ display: 'flex', gap: '24px', marginTop: '32px', justifyContent: 'center' }}>
          <div className="card floor-calls-table">
            <h4>קריאות לקומה</h4>
            <ul style={{ paddingInlineStart: 18 }}>
              {floorCalls.length === 0 && <li style={{ color: '#888' }}>אין קריאות פעילות</li>}
              {floorCalls.map(call => (
                <li key={call.id}>
                  קומה {call.requestedFloor} ({new Date(call.callTime).toLocaleTimeString()})
                </li>
              ))}
            </ul>
          </div>
          <div className="card elevator-requests-table">
            <h4>יעדים מתוך המעלית</h4>
            <ul style={{ paddingInlineStart: 18 }}>
              {elevatorRequests.length === 0 && <li style={{ color: '#888' }}>אין יעדים פעילים</li>}
              {elevatorRequests.map(call => (
                <li key={call.id}>
                  {`מקור: קומה ${call.requestedFloor} → יעד: קומה ${call.destinationFloor} (${new Date(call.callTime).toLocaleTimeString()})`}
                </li>
              ))}
            </ul>
          </div>
          <div className="card pending-calls-table">
            <h4>קריאות ממתינות</h4>
            <ul style={{ paddingInlineStart: 18 }}>
              {pendingCalls.length === 0 && <li style={{ color: '#888' }}>אין קריאות ממתינות</li>}
              {pendingCalls.map((call, idx) => (
                <li key={idx}>
                  קומה {call.floor} ({call.direction === 'up' ? '▲' : '▼'}) {new Date(call.time).toLocaleTimeString()}
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};

export default BuildingSimulationPage; 