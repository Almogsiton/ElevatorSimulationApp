using Microsoft.AspNetCore.Mvc;
using ElevatorApp.Services;
using ElevatorApp.DataAccess.Entities;

namespace ElevatorApp.Controllers
{
    /// <summary>
    /// Controller for retrieving elevator status.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ElevatorController : ControllerBase
    {
        private readonly ElevatorService _elevatorService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElevatorController"/> class.
        /// </summary>
        /// <param name="elevatorService">The service used for elevator logic.</param>
        public ElevatorController(ElevatorService elevatorService)
        {
            _elevatorService = elevatorService;
        }

        /// <summary>
        /// Gets the current status of the elevator by ID.
        /// </summary>
        /// <param name="elevatorId">The ID of the elevator.</param>
        /// <returns>The elevator details or 404 if not found.</returns>
        [HttpGet("status/{elevatorId}")]
        public IActionResult GetStatus(int elevatorId)
        {
            var elevator = _elevatorService.GetElevatorStatus(elevatorId);

            if (elevator == null)
                return NotFound($"Elevator with ID {elevatorId} not found.");

            return Ok(new
            {
                elevator.Id,
                elevator.CurrentFloor,
                Status = elevator.Status.ToString(),
                Direction = elevator.Direction.ToString(),
                DoorStatus = elevator.DoorStatus.ToString()
            });
        }
    }
}
