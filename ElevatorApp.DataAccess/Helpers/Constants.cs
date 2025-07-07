namespace ElevatorApp.DataAccess.Helpers

{
    /// <summary>
    /// Contains system-wide constant values used throughout the application.
    /// </summary>
    public static class Constants
    {
        // Default values
        public const int DefaultGroundFloor = 0;

        // Time intervals in milliseconds
        public const int SimulationIntervalMs = 2000;
        public const int DoorOpenTimeMs = 3000;

        // Number of simulation ticks to keep doors open
        public const int DoorOpenTicks = 3;

        // Status messages
        public const string StatusIdle = "Idle";
        public const string StatusMovingUp = "MovingUp";
        public const string StatusMovingDown = "MovingDown";
        public const string StatusOpeningDoors = "OpeningDoors";
        public const string StatusClosingDoors = "ClosingDoors";

        public const string DirectionUp = "Up";
        public const string DirectionDown = "Down";
        public const string DirectionNone = "None";

        public const string DoorOpen = "Open";
        public const string DoorClosed = "Closed";
    }
}
