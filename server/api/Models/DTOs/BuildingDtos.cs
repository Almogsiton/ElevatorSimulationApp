// Building DTOs - data transfer objects for building management operations
// Contains request and response models for building creation and retrieval

using System.ComponentModel.DataAnnotations;
using ElevatorSimulationApi.Config;
namespace ElevatorSimulationApi.Models.DTOs;

// Building creation request with name and floor count validation
public class CreateBuildingRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Range(AppConstants.Building.MinFloors, AppConstants.Building.MaxFloors)]
    public int NumberOfFloors { get; set; }
}

// Building response with complete building and elevator information
public class BuildingResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NumberOfFloors { get; set; }
    public int UserId { get; set; }
    public ElevatorResponse Elevator { get; set; } = null!;
}

// Building list response with basic building information
public class BuildingListResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int NumberOfFloors { get; set; }
} 