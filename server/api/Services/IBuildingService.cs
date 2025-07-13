using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IBuildingService
{
    Task<List<BuildingListResponse>> GetUserBuildingsAsync(int userId);
    Task<BuildingResponse> GetBuildingAsync(int buildingId, int userId);
    Task<BuildingResponse> CreateBuildingAsync(CreateBuildingRequest request, int userId);
} 