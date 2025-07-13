using System.ComponentModel.DataAnnotations;

namespace ElevatorSimulationApi.Models.DTOs;

public class CreateElevatorCallRequest
{
    [Required]
    public int BuildingId { get; set; }
    
    [Required]
    [Range(0, 100)]
    public int RequestedFloor { get; set; }
    
    [Range(0, 100)]
    public int? DestinationFloor { get; set; }
}

public class UpdateElevatorCallRequest
{
    [Required]
    [Range(0, 100)]
    public int DestinationFloor { get; set; }
}

public class ElevatorCallResponse
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public int RequestedFloor { get; set; }
    public int? DestinationFloor { get; set; }
    public DateTime CallTime { get; set; }
    public bool IsHandled { get; set; }
} 