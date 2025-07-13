using ElevatorSimulationApi.Models.DTOs;
using ElevatorSimulationApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorSimulationApi.Controllers;

[ApiController]
[Route("[controller]")]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingService _buildingService;

    public BuildingsController(IBuildingService buildingService)
    {
        _buildingService = buildingService;
    }

    [HttpGet("get/buildings/user/{userId}")]
    public async Task<ActionResult<List<BuildingListResponse>>> GetUserBuildings(int userId)
    {
        try
        {
            var buildings = await _buildingService.GetUserBuildingsAsync(userId);
            return Ok(buildings);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("get/{buildingId}/user/{userId}")]
    public async Task<ActionResult<BuildingResponse>> GetBuilding(int buildingId, int userId)
    {
        try
        {
            var building = await _buildingService.GetBuildingAsync(buildingId, userId);
            return Ok(building);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("create/user/{userId}")]
    public async Task<ActionResult<BuildingResponse>> CreateBuilding(CreateBuildingRequest request, int userId)
    {
        try
        {
            var building = await _buildingService.CreateBuildingAsync(request, userId);
            return CreatedAtAction(nameof(GetBuilding), new { buildingId = building.Id, userId = userId }, building);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


} 