import React from 'react';

const ElevatorShaft = ({ 
  building, 
  elevator, 
  handleCallElevator, 
  handleSelectDestination,
  handleFloorNumberCall 
}) => {
  return (
    <div className="building-shaft-container">
      <div className="building-shaft" style={{ height: 60 * building.numberOfFloors }}>
        {Array.from({ length: building.numberOfFloors }, (_, i) => building.numberOfFloors - 1 - i).map((floor) => (
          <div
            key={floor}
            className={`building-floor-row${elevator.currentFloor === floor ? ' current-floor-row' : ''}`}
          >
            <div className="floor-buttons-row" style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
              {/* Floor number button (creates new call) */}
              <button
                className="floor-btn floor-number-btn"
                onClick={() => handleFloorNumberCall(floor)}
                disabled={elevator.currentFloor === floor}
                title={elevator.currentFloor === floor ? 'You are here' : 'Call elevator to floor ' + floor}
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
  );
};

export default ElevatorShaft; 