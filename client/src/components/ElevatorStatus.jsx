import React from 'react';

// Enum mappings for backend numeric values
const statusMap = {
  0: 'Idle',
  1: 'MovingUp',
  2: 'MovingDown',
  3: 'OpeningDoors',
  4: 'ClosingDoors',
};

const directionMap = {
  0: 'Up',
  1: 'Down',
  2: 'None',
};

const doorStatusMap = {
  0: 'Closed',
  1: 'Open',
  2: 'Opening',
  3: 'Closing',
};

// פונקציות עזר להמרת ערכים למחרוזות קריאות
const getDirectionText = (direction) => {
  if (direction === 'Up' || direction === 0) return 'Up';
  if (direction === 'Down' || direction === 1) return 'Down';
  if (direction === 'None' || direction === 2) return 'None';
  return String(direction);
};

const getDoorStatusText = (doorStatus) => {
  if (doorStatus === 'Open' || doorStatus === 0) return 'Open';
  if (doorStatus === 'Closed' || doorStatus === 1) return 'Closed';
  if (doorStatus === 'Opening' || doorStatus === 2) return 'Opening';
  if (doorStatus === 'Closing' || doorStatus === 3) return 'Closing';
  return String(doorStatus);
};

const ElevatorStatus = ({ elevator }) => {
  return (
    <div className="elevator-status">
      <h3>Elevator Status</h3>
      <div style={{ marginBottom: '10px' }}>
        <span className={`status-indicator status-${String(elevator.status).toLowerCase()}`}></span>
        <strong>Status: {elevator.status}</strong>
      </div>
      <div style={{ marginBottom: '10px' }}>
        <strong>Floor: {elevator.currentFloor}</strong>
      </div>
      <div style={{ marginBottom: '10px' }}>
        <strong>Direction: {getDirectionText(elevator.direction)}</strong>
      </div>
      <div>
        <strong>Doors: {getDoorStatusText(elevator.doorStatus)}</strong>
      </div>
    </div>
  );
};

export default ElevatorStatus; 