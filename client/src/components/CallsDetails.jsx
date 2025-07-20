import React from 'react';
import '../styles/CallsDetails.css';

// Component for displaying floor calls and pending calls in a card layout

const CallsDetails = ({ 
  sortedFloorCalls, 
  sortedPendingCalls 
}) => {
  return (
    <div className="calls-details">
      {/* Floor Calls */}
      <div className="card">
        <h4>Floor Calls</h4>
        <div className="card-content">
          {sortedFloorCalls.length === 0 ? (
            <span className="no-calls">No active calls</span>
          ) : (
            sortedFloorCalls.map(call => (
              <div key={call.id} className="call-item">
                Floor {call.requestedFloor} ({new Date(call.callTime).toLocaleTimeString()})
              </div>
            ))
          )}
        </div>
      </div>

      {/* Pending Calls */}
      <div className="card">
        <h4>Pending Calls</h4>
        <div className="card-content">
          {sortedPendingCalls.length === 0 ? (
            <span className="no-calls">No pending calls</span>
          ) : (
            sortedPendingCalls.map((call, idx) => (
              <div key={call.floor + '-' + (call.direction || call.type) + '-' + call.time + '-' + idx} className="call-item">
                Floor {call.floor} ({call.direction || call.type})
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};

export default CallsDetails; 