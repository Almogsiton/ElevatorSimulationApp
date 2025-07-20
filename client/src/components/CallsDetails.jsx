import React from 'react';

const CallsDetails = ({ 
  sortedFloorCalls, 
  elevatorRequests, 
  sortedPendingCalls 
}) => {
  return (
    <div className="calls-details" style={{ 
      display: 'flex', 
      gap: '16px', 
      marginTop: '16px',
      flexWrap: 'wrap'
    }}>
      {/* Floor Calls */}
      <div className="card" style={{ flex: '1', minWidth: '200px' }}>
        <h4 style={{ margin: '0 0 8px 0', fontSize: '14px', color: '#495057' }}>Floor Calls</h4>
        <div style={{ fontSize: '12px' }}>
          {sortedFloorCalls.length === 0 ? (
            <span style={{ color: '#888' }}>No active calls</span>
          ) : (
            sortedFloorCalls.map(call => (
              <div key={call.id} style={{ marginBottom: '4px' }}>
                Floor {call.requestedFloor} ({new Date(call.callTime).toLocaleTimeString()})
              </div>
            ))
          )}
        </div>
      </div>

      {/* Elevator Destinations */}
      <div className="card" style={{ flex: '1', minWidth: '200px' }}>
        <h4 style={{ margin: '0 0 8px 0', fontSize: '14px', color: '#495057' }}>Elevator Destinations</h4>
        <div style={{ fontSize: '12px' }}>
          {elevatorRequests.length === 0 ? (
            <span style={{ color: '#888' }}>No active destinations</span>
          ) : (
            elevatorRequests.map(call => (
              <div key={call.id} style={{ marginBottom: '4px' }}>
                {`Floor ${call.requestedFloor} → ${call.destinationFloor} (${new Date(call.callTime).toLocaleTimeString()})`}
              </div>
            ))
          )}
        </div>
      </div>

      {/* Pending Calls */}
      <div className="card" style={{ flex: '1', minWidth: '200px' }}>
        <h4 style={{ margin: '0 0 8px 0', fontSize: '14px', color: '#495057' }}>Pending Calls</h4>
        <div style={{ fontSize: '12px' }}>
          {sortedPendingCalls.length === 0 ? (
            <span style={{ color: '#888' }}>No pending calls</span>
          ) : (
            sortedPendingCalls.map((call, idx) => (
              <div key={call.floor + '-' + call.direction + '-' + call.time + '-' + idx} style={{ marginBottom: '4px' }}>
                Floor {call.floor} ({call.direction})
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};

export default CallsDetails; 