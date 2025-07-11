using Microsoft.AspNetCore.Mvc;
using ElevatorApp.DataAccess.Context;
using ElevatorApp.DataAccess.Entities;
using ElevatorApp.DataAccess.Helpers;

namespace ElevatorApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildingController : ControllerBase
    {
        private readonly ElevatorDbContext _db;

        public BuildingController(ElevatorDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public IActionResult CreateBuilding([FromBody] Building building)
        {
            if (building.NumberOfFloors < Constants.MinFloorsPerBuilding ||
                building.NumberOfFloors > Constants.MaxFloorsPerBuilding)
            {
                return BadRequest($"Number of floors must be between {Constants.MinFloorsPerBuilding} and {Constants.MaxFloorsPerBuilding}.");
            }

            var nameExists = _db.Buildings
        .Any(b => b.UserId == building.UserId && b.Name == building.Name);

            if (nameExists)
            {
                return Conflict("You already have a building with this name.");
            }

            _db.Buildings.Add(building);
            _db.SaveChanges();

            var elevator = new Elevator
            {
                BuildingId = building.Id,
                CurrentFloor = Constants.DefaultGroundFloor,
                Status = ElevatorStatus.Idle,
                Direction = ElevatorDirection.None,
                DoorStatus = DoorStatus.Closed
            };

            _db.Elevators.Add(elevator);
            _db.SaveChanges();

            return Ok(new { message = "Building created", buildingId = building.Id });
        }

        [HttpGet("{userId}")]
        public IActionResult GetUserBuildings(int userId)
        {
            var buildings = _db.Buildings
                .Where(b => b.UserId == userId)
                .ToList();

            return Ok(buildings);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBuilding(int id)
        {
            var building = _db.Buildings.FirstOrDefault(b => b.Id == id);

            if (building == null)
                return NotFound("Building not found");

            // מחק גם מעלית של הבניין
            var elevator = _db.Elevators.FirstOrDefault(e => e.BuildingId == id);
            if (elevator != null)
                _db.Elevators.Remove(elevator);

            _db.Buildings.Remove(building);
            _db.SaveChanges();

            return Ok("Building deleted successfully");
        }



    }
}
