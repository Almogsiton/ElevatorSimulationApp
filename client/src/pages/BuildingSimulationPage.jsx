import React, { useState, useEffect, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import * as signalR from '@microsoft/signalr';
import { buildingService, elevatorCallService } from '../services/apiService';
import { useAuth } from '../contexts/AuthContext';
import ElevatorStatus from '../components/ElevatorStatus';
import FloorButton from '../components/FloorButton';

const BuildingSimulationPage = () => {
  const { buildingId } = useParams(); // buildingId is a string from the URL
  const numericBuildingId = Number(buildingId); // always use as number
  const navigate = useNavigate();
  const { user } = useAuth();
  const [building, setBuilding] = useState(null);
  const [elevator, setElevator] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [activeCall, setActiveCall] = useState(null);
  const [showDestinationButtons, setShowDestinationButtons] = useState(false);
  const connectionRef = useRef(null);

  useEffect(() => {
    loadBuilding();
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
        console.log('Received elevator update:', message);
        setElevator(prev => ({
          ...prev,
          currentFloor: message.currentFloor,
          status: message.status,
          direction: message.direction,
          doorStatus: message.doorStatus
        }));

        if (message.doorStatus === 'Open' && activeCall) {
          setShowDestinationButtons(true);
        }
      });

      await connection.start();
      await connection.invoke('JoinElevatorGroup', elevator.id);
      connectionRef.current = connection;
      console.log('SignalR connected successfully for elevator:', elevator.id);
    } catch (error) {
      console.error('SignalR connection failed:', error);
    }
  };

  const handleCallElevator = async (floor, direction) => {
    try {
      console.log('Creating elevator call for building:', numericBuildingId, 'floor:', floor + 1);
      const call = await elevatorCallService.createCall(numericBuildingId, floor);
      setActiveCall(call);
      setError('');
    } catch (error) {
      console.error('Elevator call creation failed:', error);
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
    } catch (error) {
      setError(error.message);
    }
  };

  const getElevatorPosition = () => {
    if (!elevator || !building) return 0;
    const floorHeight = 400 / building.numberOfFloors;
    // Position: 0 (bottom) for floor 1, up to (N-1) for top floor
    return elevator.currentFloor * floorHeight;
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
      <div className="card">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <h2>{building.name}</h2>
          <button className="btn btn-secondary" onClick={() => navigate('/buildings')}>
            Back to Buildings
          </button>
        </div>

        <div className="elevator-container">
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
              {Array.from({ length: building.numberOfFloors }, (_, i) => building.numberOfFloors - i).map((floor) => (
                <FloorButton
                  key={floor}
                  floor={floor}
                  elevator={elevator}
                  numberOfFloors={building.numberOfFloors}
                  onCallElevator={handleCallElevator}
                  onSelectDestination={handleSelectDestination}
                  showDestinationButtons={showDestinationButtons && (elevator.currentFloor + 1) === floor}
                />
              ))}
            </div>
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
        </div>
      </div>
    </div>
  );
};

export default BuildingSimulationPage; 