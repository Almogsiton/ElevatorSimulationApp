// Elevator call DTOs - data transfer objects for elevator call management
// Contains request and response models for call creation, updates, and status

using System.ComponentModel.DataAnnotations;
using ElevatorSimulationApi.Config;
namespace ElevatorSimulationApi.Models.DTOs;

// Elevator call creation request with floor and optional destination
public class CreateElevatorCallRequest
{
    [Required]
    public int BuildingId { get; set; }

    [Required]
    [Range(AppConstants.Elevator.MinFloor, AppConstants.Elevator.MaxFloor)]
    public int RequestedFloor { get; set; }

    [Range(AppConstants.Elevator.MinFloor, AppConstants.Elevator.MaxFloor)]
    public int? DestinationFloor { get; set; }
}

// Elevator call update request with destination floor
public class UpdateElevatorCallRequest
{
    [Required]
    [Range(AppConstants.Elevator.MinFloor, AppConstants.Elevator.MaxFloor)]
    public int DestinationFloor { get; set; }
}

// Elevator call response with complete call information
public class ElevatorCallResponse
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public int RequestedFloor { get; set; }
    public int? DestinationFloor { get; set; }
    public DateTime CallTime { get; set; }
    public bool IsHandled { get; set; }
} 