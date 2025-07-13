using ElevatorSimulationApi.Models.Enums;

namespace ElevatorSimulationApi.Models.DTOs;

public class ElevatorResponse
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public int CurrentFloor { get; set; }
    public ElevatorStatus Status { get; set; }
    public ElevatorDirection Direction { get; set; }
    public DoorStatus DoorStatus { get; set; }
}

public class ElevatorUpdateMessage
{
    public int ElevatorId { get; set; }
    public int CurrentFloor { get; set; }
    public ElevatorStatus Status { get; set; }
    public ElevatorDirection Direction { get; set; }
    public DoorStatus DoorStatus { get; set; }
} 