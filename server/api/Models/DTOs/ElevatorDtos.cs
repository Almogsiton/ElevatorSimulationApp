// Elevator DTOs - data transfer objects for elevator state and real-time updates
// Contains response models for elevator information and SignalR update messages

using ElevatorSimulationApi.Models.Enums;

namespace ElevatorSimulationApi.Models.DTOs;

// Elevator response with current state and position information
public class ElevatorResponse
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public int CurrentFloor { get; set; }
    public ElevatorStatus Status { get; set; }
    public ElevatorDirection Direction { get; set; }
    public DoorStatus DoorStatus { get; set; }
}

// Real-time elevator update message for SignalR communication
public class ElevatorUpdateMessage
{
    public int ElevatorId { get; set; }
    public int CurrentFloor { get; set; }
    public ElevatorStatus Status { get; set; }
    public ElevatorDirection Direction { get; set; }
    public DoorStatus DoorStatus { get; set; }
} 