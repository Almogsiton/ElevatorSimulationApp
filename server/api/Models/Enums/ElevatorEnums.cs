namespace ElevatorSimulationApi.Models.Enums;

public enum ElevatorStatus
{
    Idle,
    MovingUp,
    MovingDown,
    OpeningDoors,
    ClosingDoors
}

public enum ElevatorDirection
{
    Up,
    Down,
    None
}

public enum DoorStatus
{
    Open,
    Closed,
    Opening,
    Closing
} 