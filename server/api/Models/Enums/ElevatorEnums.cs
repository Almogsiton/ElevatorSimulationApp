// Elevator enums - enumeration types for elevator state management
// Defines status, direction, and door state values for elevator operations

namespace ElevatorSimulationApi.Models.Enums;

// Elevator movement and operation status values
public enum ElevatorStatus
{
    Idle,
    MovingUp,
    MovingDown,
    OpeningDoors,
    ClosingDoors
}

// Elevator movement direction values
public enum ElevatorDirection
{
    Up,
    Down,
    None
}

// Elevator door state values
public enum DoorStatus
{
    Open,
    Closed,
    Opening,
    Closing
} 