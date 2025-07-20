// Application constants - centralized configuration for magic numbers and validation rules
// Contains all hardcoded values used throughout the application

namespace ElevatorSimulationApi.Config;

public static class AppConstants
{
    // Authentication constants
    public static class Auth
    {
        public const int MinPasswordLength = 6;
        public const int MaxEmailLength = 255;
    }

    // Building constants
    public static class Building
    {
        public const int MinFloors = 1;
        public const int MaxFloors = 100;
        public const int MaxNameLength = 255;
    }

    // Elevator constants
    public static class Elevator
    {
        public const int MinFloor = 0;
        public const int MaxFloor = 100;
    }

    // Database constants
    public static class Database
    {
        public const int MaxStringLength = 255;
    }

    // Simulation constants
    public static class Simulation
    {
        public const int DefaultIntervalSeconds = 20;
    }
} 