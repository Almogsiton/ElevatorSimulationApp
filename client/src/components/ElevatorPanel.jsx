import React from 'react';
import '../styles/ElevatorPanel.css';

const ElevatorPanel = ({ 
  numberOfFloors, 
  currentFloor, 
  onSelectDestination, 
  activeDestinations = [],
  isVisible = false 
}) => {
  if (!isVisible) return null;

  return (
    <div className="elevator-panel">
      <div className="elevator-panel-header">
        <h4>Elevator Panel</h4>
        <div className="current-floor-display">
          Floor: {currentFloor}
        </div>
      </div>
      <div className="elevator-buttons-grid">
        {Array.from({ length: numberOfFloors }, (_, i) => numberOfFloors - 1 - i).map((floor) => {
          const isActive = activeDestinations.includes(floor);
          const isCurrentFloor = currentFloor === floor;
          
          return (
            <button
              key={floor}
              className={`elevator-floor-btn ${isActive ? 'active' : ''} ${isCurrentFloor ? 'current' : ''}`}
              onClick={() => onSelectDestination(floor)}
              title={isCurrentFloor ? 'You are already on this floor' : `Go to floor ${floor}`}
            >
              {floor}
            </button>
          );
        })}
      </div>
      <div className="elevator-panel-footer">
        <div className="door-status">
          Doors: {isVisible ? 'Open' : 'Closed'}
        </div>
      </div>
    </div>
  );
};

export default ElevatorPanel; 