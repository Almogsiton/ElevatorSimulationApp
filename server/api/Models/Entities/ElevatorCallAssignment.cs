// Elevator call assignment entity - represents assignment of calls to specific elevators
// Contains relationship between elevators and calls with assignment timing

namespace ElevatorSimulationApi.Models.Entities;

public class ElevatorCallAssignment
{
    public int Id { get; set; }
    
    public int ElevatorId { get; set; }
    public virtual Elevator Elevator { get; set; } = null!;
    
    public int ElevatorCallId { get; set; }
    public virtual ElevatorCall ElevatorCall { get; set; } = null!;
    
    public DateTime AssignmentTime { get; set; }
} 