using System.ComponentModel.DataAnnotations;

namespace ElevatorSimulationApi.Models.Entities;

public class Building
{
    public int Id { get; set; }
    
    [Required]
    public string Name { get; set; } = string.Empty;
    
    public int NumberOfFloors { get; set; }
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    public virtual Elevator Elevator { get; set; } = null!;
    
    public virtual ICollection<ElevatorCall> ElevatorCalls { get; set; } = new List<ElevatorCall>();
} 