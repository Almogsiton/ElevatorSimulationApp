namespace ElevatorApp.DataAccess.Helpers

{
    /// <summary>
    /// Represents the current status of the elevator.
    /// </summary>
    public enum ElevatorStatus
    {
        Idle,
        MovingUp,
        MovingDown,
        OpeningDoors,
        ClosingDoors
    }

    /// <summary>
    /// Represents the direction the elevator is moving.
    /// </summary>
    public enum ElevatorDirection
    {
        None,
        Up,
        Down
    }

    /// <summary>
    /// Represents the status of the elevator doors.
    /// </summary>
    public enum DoorStatus
    {
        Closed,
        Open
    }
}
