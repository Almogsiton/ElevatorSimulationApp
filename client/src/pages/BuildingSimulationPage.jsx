import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { buildingService, elevatorCallService } from '../services/apiService';
import { useAuth } from '../contexts/AuthContext';
import ElevatorStatus from '../components/ElevatorStatus';

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

  // load all the active calls 
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

  // פונקציית עדיפות לקריאה ממתינה
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
    // מניעת כפילות ב-pendingCalls
    if (pendingCalls.some(c => c.floor === floor && c.direction === direction)) {
      showToast('כבר קיימת קריאה ממתינה לאותה קומה וכיוון');
      return;
    }
    // מניעת כפילות בקריאות פעילות
    if (floorCalls.some(c => c.requestedFloor === floor && c.direction === direction)) {
      showToast('כבר קיימת קריאה פעילה לאותה קומה וכיוון');
      return;
    }
    if (!isCallOnTheWay(floor, direction)) {
      // הכנסה חכמה למקום הנכון
      setPendingCalls(prev => {
        const newCall = { floor, direction, time: Date.now() };
        // מניעת כפילות (ליתר ביטחון)
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
    if (!elevator) return true;
    if (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2) return true;
    const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
    
    // עולה
    if (elevator.direction === 'Up' || elevator.direction === 0) {
      // קריאה לרדת מהקומה העליונה
      if (floor === maxFloor && direction === 'down') return true;
      // קריאה לעלות מקומה גבוהה מהמעלית
      if (direction === 'up' && floor > elevator.currentFloor) return true;
      return false;
    }
    
    // יורדת
    if (elevator.direction === 'Down' || elevator.direction === 1) {
      // קריאה לעלות מהקומה התחתונה
      if (floor === 0 && direction === 'up') return true;
      // קריאה לרדת מקומה נמוכה מהמעלית
      if (direction === 'down' && floor < elevator.currentFloor) return true;
      return false;
    }
    
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
    const isElevatorFree = (elevator.status === 'Idle' || elevator.direction === 'None' || elevator.direction === 2);
    if (isElevatorFree && floorCalls.length === 0 && elevatorRequests.length === 0 && pendingCalls.length > 0) {
      const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
      
      // בדוק אם כל הקריאות הממתינות הן באותו כיוון
      const allUp = pendingCalls.every(call => call.direction === 'up');
      const allDown = pendingCalls.every(call => call.direction === 'down');
      
      let newDirection = null;
      
      if (allUp) {
        newDirection = 'up';
      } else if (allDown) {
        newDirection = 'down';
      } else {
        // ערבוב כיוונים – נדרש לוגיקה חכמה יותר
        // קודם כל, בדוק אם יש קריאות לירידה מהקומה העליונה
        const downFromTop = pendingCalls.some(call => call.direction === 'down' && call.floor === maxFloor);
        // בדוק אם יש קריאות לעליה מהקומה התחתונה
        const upFromBottom = pendingCalls.some(call => call.direction === 'up' && call.floor === 0);
        
        if (downFromTop && elevator.currentFloor === maxFloor) {
          // המעלית בקומה העליונה ויש קריאה לירידה - תרד
          newDirection = 'down';
        } else if (upFromBottom && elevator.currentFloor === 0) {
          // המעלית בקומה התחתונה ויש קריאה לעליה - תעלה
          newDirection = 'up';
        } else {
          // קבע לפי הקריאה הקרובה ביותר למעלית
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
      
      // שחרר רק קריאות שתואמות לכיוון החדש
      const callsToSend = [];
      const callsToKeep = [];
      
      pendingCalls.forEach(call => {
        if (
          (newDirection === 'up' && call.direction === 'up' && call.floor > elevator.currentFloor) ||
          (newDirection === 'up' && call.direction === 'down' && call.floor === maxFloor) ||
          (newDirection === 'down' && call.direction === 'down' && call.floor < elevator.currentFloor) ||
          (newDirection === 'down' && call.direction === 'up' && call.floor === 0) ||
          // מקרה קצה: כל הקריאות באותו כיוון, אז תשחרר את כולן
          (allUp && newDirection === 'up' && call.direction === 'up') ||
          (allDown && newDirection === 'down' && call.direction === 'down')
        ) {
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
      // קבע את הכיוון החדש
      const newDirection = elevator.direction === 0 || elevator.direction === 'Up' ? 'up'
        : elevator.direction === 1 || elevator.direction === 'Down' ? 'down'
        : null;
      if (!newDirection) return;
      const maxFloor = building?.numberOfFloors ? building.numberOfFloors - 1 : 0;
      const callsToSend = [];
      const callsToKeep = [];
      pendingCalls.forEach(call => {
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
    prevDirectionRef.current = elevator.direction;
    // eslint-disable-next-line
  }, [elevator?.direction]);

  // מיון רשימות להצגה
  const sortedFloorCalls = [...floorCalls].sort((a, b) => a.requestedFloor - b.requestedFloor);
  // מיון חכם של קריאות ממתינות
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
              {sortedFloorCalls.length === 0 && <li style={{ color: '#888' }}>אין קריאות פעילות</li>}
              {sortedFloorCalls.map(call => (
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
              {sortedPendingCalls.length === 0 && <li style={{ color: '#888' }}>אין קריאות ממתינות</li>}
              {sortedPendingCalls.map((call, idx) => (
                <li key={call.floor + '-' + call.direction + '-' + call.time + '-' + idx}>
                  קומה {call.floor} ({call.direction})
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