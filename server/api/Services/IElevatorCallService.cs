using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IElevatorCallService
{
    Task<ElevatorCallResponse> CreateCallAsync(CreateElevatorCallRequest request);
    Task<ElevatorCallResponse> UpdateCallAsync(int callId, UpdateElevatorCallRequest request);
    Task<List<ElevatorCallResponse>> GetBuildingCallsAsync(int buildingId);
} 