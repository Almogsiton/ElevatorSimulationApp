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
  0: 'None',
  1: 'Up',
  2: 'Down',
};

const doorStatusMap = {
  0: 'Closed',
  1: 'Open',
  2: 'Opening',
  3: 'Closing',
};

const ElevatorStatus = ({ elevator }) => {
  // Map numeric values to string keys if needed
  const status = typeof elevator.status === 'number' ? statusMap[elevator.status] : elevator.status;
  const direction = typeof elevator.direction === 'number' ? directionMap[elevator.direction] : elevator.direction;
  const doorStatus = typeof elevator.doorStatus === 'number' ? doorStatusMap[elevator.doorStatus] : elevator.doorStatus;

  const getStatusClass = (status) => {
    switch (status) {
      case 'Idle': return 'status-idle';
      case 'MovingUp':
      case 'MovingDown': return 'status-moving';
      case 'OpeningDoors':
      case 'ClosingDoors': return 'status-opening';
      default: return 'status-idle';
    }
  };

  const getStatusText = (status) => {
    switch (status) {
      case 'Idle': return 'Idle';
      case 'MovingUp': return 'Moving Up';
      case 'MovingDown': return 'Moving Down';
      case 'OpeningDoors': return 'Opening Doors';
      case 'ClosingDoors': return 'Closing Doors';
      default: return 'Unknown';
    }
  };

  const getDirectionIcon = (direction) => {
    switch (direction) {
      case 'Up': return '↑';
      case 'Down': return '↓';
      case 'None': return '●';
      default: return '●';
    }
  };

  const getDoorStatusText = (doorStatus) => {
    switch (doorStatus) {
      case 'Open': return 'Doors Open';
      case 'Closed': return 'Doors Closed';
      case 'Opening': return 'Doors Opening';
      case 'Closing': return 'Doors Closing';
      default: return 'Unknown';
    }
  };

  return (
    <div className="elevator-status">
      <h3>Elevator Status</h3>
      <div style={{ marginBottom: '10px' }}>
        <span className={`status-indicator ${getStatusClass(status)}`}></span>
        <strong>{getStatusText(status)}</strong>
      </div>
      <div style={{ marginBottom: '10px' }}>
        <span className="direction-indicator">{getDirectionIcon(direction)}</span>
        <strong>Floor {elevator.currentFloor + 1}</strong>
      </div>
      <div>
        <strong>{getDoorStatusText(doorStatus)}</strong>
      </div>
    </div>
  );
};

export default ElevatorStatus; 