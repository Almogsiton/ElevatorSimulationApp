import React from 'react';
import '../styles/ElevatorStatus.css';
import { 
  STATUS_TEXT_MAP, 
  DIRECTION_TEXT_MAP, 
  DOOR_STATUS_TEXT_MAP,
  ELEVATOR_STATUS,
  ELEVATOR_DIRECTION,
  DOOR_STATUS
} from '../config/config';

// Component to display current elevator status (status, floor, direction, doors)
const getStatusText = (status) => {
  if (typeof status === 'string') return status;
  return STATUS_TEXT_MAP[status] || String(status);
};

const getDirectionText = (direction) => {
  if (direction === 'Up' || direction === ELEVATOR_DIRECTION.UP) return 'Up';
  if (direction === 'Down' || direction === ELEVATOR_DIRECTION.DOWN) return 'Down';
  if (direction === 'None' || direction === ELEVATOR_DIRECTION.NONE) return 'None';
  return String(direction);
};

const getDoorStatusText = (doorStatus) => {
  if (doorStatus === 'Open' || doorStatus === DOOR_STATUS.OPEN) return 'Open';
  if (doorStatus === 'Closed' || doorStatus === DOOR_STATUS.CLOSED) return 'Closed';
  if (doorStatus === 'Opening' || doorStatus === DOOR_STATUS.OPENING) return 'Opening';
  if (doorStatus === 'Closing' || doorStatus === DOOR_STATUS.CLOSING) return 'Closing';
  return String(doorStatus);
};

const ElevatorStatus = ({ elevator }) => {
  return (
    <div className="elevator-status">
      <h3>Elevator Status</h3>
      <div>
        <span className={`status-indicator status-${String(elevator.status).toLowerCase()}`}></span>
        <strong>Status: {getStatusText(elevator.status)}</strong>
      </div>
      <div>
        <strong>Floor: {elevator.currentFloor}</strong>
      </div>
      <div>
        <strong>Direction: {getDirectionText(elevator.direction)}</strong>
      </div>
      <div>
        <strong>Doors: {getDoorStatusText(elevator.doorStatus)}</strong>
      </div>
    </div>
  );
};

export default ElevatorStatus; 