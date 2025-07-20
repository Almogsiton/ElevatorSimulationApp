import React from 'react';
import '../styles/ElevatorStatusDisplay.css';

const ElevatorStatusDisplay = ({ elevator }) => {
  if (!elevator) return null;

  const getStatusText = (status) => {
    // Handle both string and numeric values
    if (status === 'Idle' || status === 0) return 'Idle';
    if (status === 'MovingUp' || status === 1) return 'Moving Up';
    if (status === 'MovingDown' || status === 2) return 'Moving Down';
    if (status === 'OpeningDoors' || status === 3) return 'Opening Doors';
    if (status === 'ClosingDoors' || status === 4) return 'Closing Doors';
    return String(status); // Fallback to show the actual value
  };

  const getDirectionText = (direction) => {
    // Handle both string and numeric values
    if (direction === 'Up' || direction === 0) return 'Up';
    if (direction === 'Down' || direction === 1) return 'Down';
    if (direction === 'None' || direction === 2) return 'None';
    return String(direction); // Fallback to show the actual value
  };

  const getDoorStatusText = (doorStatus) => {
    // Handle both string and numeric values
    if (doorStatus === 'Open' || doorStatus === 0) return 'Open';
    if (doorStatus === 'Closed' || doorStatus === 1) return 'Closed';
    if (doorStatus === 'Opening' || doorStatus === 2) return 'Opening';
    if (doorStatus === 'Closing' || doorStatus === 3) return 'Closing';
    return String(doorStatus); // Fallback to show the actual value
  };

  return (
    <div className="elevator-status-display">
      <h3>Elevator Status</h3>
      <div className="status-grid">
        <div className="status-item">
          <span className="status-label">Status:</span>
          <span className="status-value">{getStatusText(elevator.status)}</span>
        </div>
        <div className="status-item">
          <span className="status-label">Floor:</span>
          <span className="status-value">{elevator.currentFloor}</span>
        </div>
        <div className="status-item">
          <span className="status-label">Direction:</span>
          <span className="status-value">{getDirectionText(elevator.direction)}</span>
        </div>
        <div className="status-item">
          <span className="status-label">Doors:</span>
          <span className="status-value">{getDoorStatusText(elevator.doorStatus)}</span>
        </div>
      </div>
    </div>
  );
};

export default ElevatorStatusDisplay; 