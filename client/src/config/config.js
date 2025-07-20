// API Configuration
export const API_CONFIG = {
  BASE_URL: 'http://localhost:5091',
  SIGNALR_HUB_URL: 'http://localhost:5091/elevatorHub'
};

// Building Configuration
export const BUILDING_CONFIG = {
  MIN_FLOORS: 2,
  MAX_FLOORS: 20
};

// Elevator status constants
export const ELEVATOR_STATUS = {
  IDLE: 0,
  MOVING_UP: 1,
  MOVING_DOWN: 2,
  OPENING_DOORS: 3,
  CLOSING_DOORS: 4
};

// Elevator direction constants
export const ELEVATOR_DIRECTION = {
  UP: 0,
  DOWN: 1,
  NONE: 2
};

// Door status constants
export const DOOR_STATUS = {
  CLOSED: 0,
  OPEN: 1,
  OPENING: 2,
  CLOSING: 3
};

// Status text mappings
export const STATUS_TEXT_MAP = {
  [ELEVATOR_STATUS.IDLE]: 'Idle',
  [ELEVATOR_STATUS.MOVING_UP]: 'Moving Up',
  [ELEVATOR_STATUS.MOVING_DOWN]: 'Moving Down',
  [ELEVATOR_STATUS.OPENING_DOORS]: 'Opening Doors',
  [ELEVATOR_STATUS.CLOSING_DOORS]: 'Closing Doors'
};

// Direction text mappings
export const DIRECTION_TEXT_MAP = {
  [ELEVATOR_DIRECTION.UP]: 'Up',
  [ELEVATOR_DIRECTION.DOWN]: 'Down',
  [ELEVATOR_DIRECTION.NONE]: 'None'
};

// Door status text mappings
export const DOOR_STATUS_TEXT_MAP = {
  [DOOR_STATUS.CLOSED]: 'Closed',
  [DOOR_STATUS.OPEN]: 'Open',
  [DOOR_STATUS.OPENING]: 'Opening',
  [DOOR_STATUS.CLOSING]: 'Closing'
};

// Elevator simulation constants
export const ELEVATOR_SIMULATION_CONFIG = {
  TOAST_DURATION: 2500,
  FLOOR_HEIGHT: 60,
  SHAFT_PADDING: 8,
  SIGNALR_URL: 'http://localhost:5091/elevatorHub'
};

