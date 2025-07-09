using Microsoft.AspNetCore.Mvc;
using ElevatorApp.Services;
using ElevatorApp.DataAccess.Entities;
using ElevatorApp.DataAccess.Helpers;

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

        /// <summary>
        /// Adds a destination floor to an elevator's target list.
        /// </summary>
        /// <param name="elevatorId">The ID of the elevator.</param>
        /// <param name="floor">The requested destination floor.</param>
        /// <returns>Status of the operation.</returns>
        [HttpPost("add-destination")]
        public IActionResult AddDestination([FromQuery] int elevatorId, [FromQuery] int floor)
        {
            var elevator = _elevatorService.GetElevatorStatus(elevatorId);

            if (elevator == null)
                return NotFound($"Elevator with ID {elevatorId} not found.");

            if (elevator.TargetFloors.Contains(floor))
                return BadRequest($"Floor {floor} is already in the target list.");

            elevator.TargetFloors.Add(floor);

            if (elevator.Direction == ElevatorDirection.Up)
                elevator.TargetFloors = elevator.TargetFloors.Distinct().OrderBy(f => f).ToList();
            else if (elevator.Direction == ElevatorDirection.Down)
                elevator.TargetFloors = elevator.TargetFloors.Distinct().OrderByDescending(f => f).ToList();

            return Ok($"Destination floor {floor} added to elevator {elevatorId}.");
        }

    }
}
