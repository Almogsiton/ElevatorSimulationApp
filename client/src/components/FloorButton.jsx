import React from 'react';

const FloorButton = ({ floor, onCallElevator, elevator, onSelectDestination, showDestinationButtons, numberOfFloors }) => {
  // floor is 1-based for display, but backend expects 0-based
  const isCurrentFloor = (elevator.currentFloor + 1) === floor;
  const isElevatorAtFloor = (elevator.currentFloor + 1) === floor && elevator.doorStatus === 'Open';

  const handleUpCall = () => {
    onCallElevator(floor - 1, 'up'); // send zero-based
  };

  const handleDownCall = () => {
    onCallElevator(floor - 1, 'down'); // send zero-based
  };

  const handleDestinationSelect = (destinationFloor) => {
    onSelectDestination(destinationFloor - 1); // send zero-based
  };

  return (
    <div className="floor">
      <div className="floor-number">{floor}</div>
      <div className="floor-buttons">
        {!showDestinationButtons ? (
          <>
            <button
              className="floor-btn up"
              onClick={handleUpCall}
              disabled={isCurrentFloor}
              title="Call elevator up"
            >
              ▲
            </button>
            <button
              className="floor-btn down"
              onClick={handleDownCall}
              disabled={isCurrentFloor}
              title="Call elevator down"
            >
              ▼
            </button>
          </>
        ) : (
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '4px' }}>
            {Array.from({ length: numberOfFloors }, (_, i) => i + 1).map((destFloor) => (
              <button
                key={destFloor}
                className="floor-btn"
                style={{ 
                  width: '30px', 
                  height: '30px', 
                  fontSize: '12px',
                  background: destFloor === floor ? '#6c757d' : '#667eea'
                }}
                onClick={() => handleDestinationSelect(destFloor)}
                disabled={destFloor === floor}
              >
                {destFloor}
              </button>
            ))}
          </div>
        )}
      </div>
      {isElevatorAtFloor && (
        <div style={{ marginLeft: '10px', color: '#28a745', fontWeight: 'bold' }}>
          ELEVATOR HERE
        </div>
      )}
    </div>
  );
};

export default FloorButton; 