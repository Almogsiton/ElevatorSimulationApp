// Elevator entity - represents elevator state and movement information
// Contains current position, status, direction, door state, and call assignments

using ElevatorSimulationApi.Models.Enums;

namespace ElevatorSimulationApi.Models.Entities;

public class Elevator
{
    public int Id { get; set; }
    
    public int BuildingId { get; set; }
    public virtual Building Building { get; set; } = null!;
    
    public int CurrentFloor { get; set; }
    
    public ElevatorStatus Status { get; set; }
    
    public ElevatorDirection Direction { get; set; }
    
    public DoorStatus DoorStatus { get; set; }
    
    public virtual ICollection<ElevatorCallAssignment> CallAssignments { get; set; } = new List<ElevatorCallAssignment>();
} 