// Building service interface - defines building management operations
// Handles building CRUD operations and user-specific building queries

using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IBuildingService
{
    // Get all buildings owned by a specific user
    Task<List<BuildingListResponse>> GetUserBuildingsAsync(int userId);
    // Get specific building with elevator details
    Task<BuildingResponse> GetBuildingAsync(int buildingId, int userId);
    // Create new building for a user
    Task<BuildingResponse> CreateBuildingAsync(CreateBuildingRequest request, int userId);
    // Get all buildings using Dapper for performance
    Task<List<ElevatorSimulationApi.Models.Entities.Building>> GetAllBuildingsWithDapperAsync();
} 