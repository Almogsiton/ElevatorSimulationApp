// Elevator call service interface - defines elevator call management operations
// Handles call creation, updates, and building-specific call queries

using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IElevatorCallService
{
    // Create new elevator call with floor and optional destination
    Task<ElevatorCallResponse> CreateCallAsync(CreateElevatorCallRequest request);
    // Update existing elevator call with destination floor
    Task<ElevatorCallResponse> UpdateCallAsync(int callId, UpdateElevatorCallRequest request);
    // Get all elevator calls for a specific building
    Task<List<ElevatorCallResponse>> GetBuildingCallsAsync(int buildingId);
} 