using Microsoft.AspNetCore.Mvc;
using ElevatorApp.DataAccess.Helpers;

namespace ElevatorApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        [HttpGet("floor-limits")]
        public IActionResult GetFloorLimits()
        {
            return Ok(new
            {
                minFloors = Constants.MinFloorsPerBuilding,
                maxFloors = Constants.MaxFloorsPerBuilding
            });
        }
    }
}
