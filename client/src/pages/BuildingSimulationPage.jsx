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

  // הוסף פונקציה לבדיקת יעד פעיל
  const isDestinationActive = (floor) => {
    return elevatorRequests.some(c => c.destinationFloor + 1 === floor);
  };

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
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h2>{building.name}</h2>
          <button className="btn btn-secondary" onClick={() => navigate('/buildings')}>
            Back to Buildings
          </button>
        </div>
        <div style={{ display: 'flex', gap: '40px', alignItems: 'flex-start' }}>
          {/* עמודת הבקשות */}
          <div style={{ minWidth: 220 }}>
            <div className="card" style={{ marginBottom: 16 }}>
              <h4>קריאות מהקומות</h4>
              <ul style={{ paddingInlineStart: 18 }}>
                {floorCalls.length === 0 && <li style={{ color: '#888' }}>אין קריאות פעילות</li>}
                {floorCalls.map(call => (
                  <li key={call.id}>
                    קומה {call.requestedFloor + 1} ({new Date(call.callTime).toLocaleTimeString()})
                  </li>
                ))}
              </ul>
            </div>
            <div className="card">
              <h4>יעדים מתוך המעלית</h4>
              <ul style={{ paddingInlineStart: 18 }}>
                {elevatorRequests.length === 0 && <li style={{ color: '#888' }}>אין יעדים פעילים</li>}
                {elevatorRequests.map(call => (
                  <li key={call.id}>
                    {`מקור: קומה ${call.requestedFloor + 1} → יעד: קומה ${call.destinationFloor + 1} (${new Date(call.callTime).toLocaleTimeString()})`}
                  </li>
                ))}
              </ul>
            </div>
          </div>
          {/* עמודת הבניין והכפתורים */}
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '40px' }}>
            <div>
              <ElevatorStatus elevator={elevator} />
              <div className="elevator-shaft">
                <div
                  className="elevator"
                  style={{ bottom: getElevatorPosition() }}
                >
                  {elevator.currentFloor + 1}
                </div>
              </div>
            </div>
            <div className="floors-container">
              {Array.from({ length: building.numberOfFloors }, (_, i) => building.numberOfFloors - i).map((floor) => {
                const isCurrent = elevator.currentFloor + 1 === floor;
                const hasCall = floorCalls.some(c => c.requestedFloor + 1 === floor);
                const hasDest = elevatorRequests.some(c => c.destinationFloor + 1 === floor);
                return (
                  <div key={floor} style={{
                    border: isCurrent ? '3px solid #28a745' : hasCall ? '3px solid #ffc107' : hasDest ? '3px solid #007bff' : '1px solid #eee',
                    background: isCurrent ? '#e6ffe6' : hasCall ? '#fffbe6' : hasDest ? '#e6f0ff' : 'white',
                    borderRadius: 10,
                    boxShadow: isCurrent ? '0 0 8px #28a74555' : hasCall ? '0 0 8px #ffc10755' : hasDest ? '0 0 8px #007bff55' : '0 1px 2px #0001',
                    marginBottom: 6,
                    transition: 'all 0.3s',
                  }}>
                    <FloorButton
                      floor={floor}
                      elevator={elevator}
                      numberOfFloors={building.numberOfFloors}
                      onCallElevator={handleCallElevator}
                      onSelectDestination={handleSelectDestination}
                      showDestinationButtons={showDestinationButtons && (elevator.currentFloor + 1) === floor}
                      isTopFloor={floor === building.numberOfFloors}
                      isBottomFloor={floor === 1}
                    />
                  </div>
                );
              })}
            </div>
            {/* כפתורי הקומות של המעלית (בחירת יעד) */}
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginRight: 24 }}>
              <div style={{ fontWeight: 600, marginBottom: 8 }}>יעדים במעלית</div>
              {Array.from({ length: building.numberOfFloors }, (_, i) => i + 1).map((floor) => (
                <button
                  key={floor}
                  className="floor-btn"
                  style={{
                    width: 40,
                    height: 40,
                    marginBottom: 6,
                    background: isDestinationActive(floor) ? '#007bff' : '#667eea',
                    color: 'white',
                    fontWeight: isDestinationActive(floor) ? 700 : 500,
                    border: isDestinationActive(floor) ? '2px solid #0056b3' : 'none',
                    fontSize: 18,
                    opacity: elevator.doorStatus === 'Open' ? 1 : 0.5,
                    cursor: elevator.doorStatus === 'Open' && !isDestinationActive(floor) && (elevator.currentFloor + 1 !== floor) ? 'pointer' : 'not-allowed',
                    pointerEvents: elevator.doorStatus === 'Open' && !isDestinationActive(floor) && (elevator.currentFloor + 1 !== floor) ? 'auto' : 'none',
                  }}
                  disabled={elevator.doorStatus !== 'Open' || isDestinationActive(floor) || (elevator.currentFloor + 1 === floor)}
                  onClick={() => handleSelectDestination(floor - 1)}
                  title={isDestinationActive(floor) ? 'יעד כבר קיים' : (elevator.currentFloor + 1 === floor ? 'אתה כבר בקומה הזו' : 'בחר יעד')}
                >
                  {floor}
                </button>
              ))}
            </div>
          </div>
        </div>
        {activeCall && (
          <div className="card" style={{ marginTop: '20px' }}>
            <h3>Active Call</h3>
            <p>Calling elevator to floor {activeCall.requestedFloor + 1}</p>
            {elevator.doorStatus === 'Open' && elevator.currentFloor === activeCall.requestedFloor && (
              <p style={{ color: '#28a745', fontWeight: 'bold' }}>
                Elevator arrived! Select your destination floor.
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default BuildingSimulationPage; 