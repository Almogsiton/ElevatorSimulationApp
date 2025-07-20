import React from 'react';
import ElevatorStatus from './ElevatorStatus';

const ElevatorMetadata = ({ 
  elevator, 
  activeCall, 
  sortedFloorCalls, 
  elevatorRequests, 
  sortedPendingCalls 
}) => {
  return (
    <div className="elevator-metadata" style={{
      display: 'flex',
      gap: '16px',
      alignItems: 'center',
      padding: '12px 16px',
      backgroundColor: '#f8f9fa',
      borderRadius: '8px',
      marginBottom: '16px',
      flexWrap: 'wrap'
    }}>
      {/* Elevator Status */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <span style={{ fontWeight: 'bold', color: '#495057' }}>Status:</span>
        <ElevatorStatus elevator={elevator} />
      </div>

      {/* Active Call */}
      {activeCall && (
        <div style={{ 
          display: 'flex', 
          alignItems: 'center', 
          gap: '8px',
          padding: '4px 8px',
          backgroundColor: '#e3f2fd',
          borderRadius: '4px',
          border: '1px solid #2196f3'
        }}>
          <span style={{ fontWeight: 'bold', color: '#1976d2' }}>Active:</span>
          <span style={{ color: '#1976d2' }}>Floor {activeCall.requestedFloor}</span>
        </div>
      )}

      {/* Floor Calls */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <span style={{ fontWeight: 'bold', color: '#495057' }}>Floor Calls:</span>
        <span style={{ 
          padding: '2px 6px', 
          backgroundColor: sortedFloorCalls.length > 0 ? '#fff3cd' : '#e9ecef',
          borderRadius: '4px',
          fontSize: '14px',
          color: sortedFloorCalls.length > 0 ? '#856404' : '#6c757d'
        }}>
          {sortedFloorCalls.length}
        </span>
      </div>

      {/* Elevator Destinations */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <span style={{ fontWeight: 'bold', color: '#495057' }}>Destinations:</span>
        <span style={{ 
          padding: '2px 6px', 
          backgroundColor: elevatorRequests.length > 0 ? '#d1ecf1' : '#e9ecef',
          borderRadius: '4px',
          fontSize: '14px',
          color: elevatorRequests.length > 0 ? '#0c5460' : '#6c757d'
        }}>
          {elevatorRequests.length}
        </span>
      </div>

      {/* Pending Calls */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <span style={{ fontWeight: 'bold', color: '#495057' }}>Pending:</span>
        <span style={{ 
          padding: '2px 6px', 
          backgroundColor: sortedPendingCalls.length > 0 ? '#f8d7da' : '#e9ecef',
          borderRadius: '4px',
          fontSize: '14px',
          color: sortedPendingCalls.length > 0 ? '#721c24' : '#6c757d'
        }}>
          {sortedPendingCalls.length}
        </span>
      </div>

      {/* Current Floor Indicator */}
      <div style={{ 
        display: 'flex', 
        alignItems: 'center', 
        gap: '8px',
        marginLeft: 'auto'
      }}>
        <span style={{ fontWeight: 'bold', color: '#495057' }}>Current:</span>
        <span style={{ 
          padding: '4px 8px', 
          backgroundColor: '#28a745',
          color: 'white',
          borderRadius: '4px',
          fontSize: '16px',
          fontWeight: 'bold'
        }}>
          {elevator?.currentFloor || 0}
        </span>
      </div>
    </div>
  );
};

export default ElevatorMetadata; 