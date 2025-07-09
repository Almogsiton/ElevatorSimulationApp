using Microsoft.AspNetCore.Mvc;
using ElevatorApp.Models;
using ElevatorApp.Services;

namespace ElevatorApp.Controllers
{
    /// <summary>
    /// Controller for managing buildings and related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingController : ControllerBase
    {
        private readonly BuildingService _buildingService;

        public BuildingController(BuildingService buildingService)
        {
            _buildingService = buildingService;
        }

        /// <summary>
        /// Returns the list of buildings owned by a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of buildings.</returns>
        [HttpGet("byUser/{userId}")]
        public IActionResult GetBuildings(int userId)
        {
            var buildings = _buildingService.GetBuildingsByUser(userId);
            return Ok(buildings);
        }

        /// <summary>
        /// Creates a new building for the user and adds an elevator automatically.
        /// </summary>
        /// <param name="request">The building creation request.</param>
        /// <returns>The created building.</returns>
        [HttpPost("create")]
        public IActionResult CreateBuilding(CreateBuildingRequest request)
        {
            var building = _buildingService.CreateBuilding(request.UserId, request.Name, request.NumberOfFloors);
            return Ok(building);
        }

        /// <summary>
        /// Returns the building with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the building.</param>
        /// <returns>The building details.</returns>
        [HttpGet("{id}")]
        public IActionResult GetBuildingById(int id)
        {
            var building = _buildingService.GetBuildingById(id);
            if (building == null)
                return NotFound();

            return Ok(building);
        }

    }
}
