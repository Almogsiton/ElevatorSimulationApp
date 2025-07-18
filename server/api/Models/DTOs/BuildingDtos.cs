using System.ComponentModel.DataAnnotations;
namespace ElevatorSimulationApi.Models.DTOs;

// TODO -> take range from config 
public class CreateBuildingRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(1, 100)]
    public int NumberOfFloors { get; set; }
}

public class BuildingResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NumberOfFloors { get; set; }
    public int UserId { get; set; }
    public ElevatorResponse Elevator { get; set; } = null!;
}

public class BuildingListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NumberOfFloors { get; set; }
} 