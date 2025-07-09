using Microsoft.AspNetCore.Mvc;
using ElevatorApp.Models;
using ElevatorApp.Services;

namespace ElevatorApp.Controllers
{
    /// <summary>
    /// Controller for creating elevator calls.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ElevatorCallController : ControllerBase
    {
        private readonly CallService _callService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElevatorCallController"/> class.
        /// </summary>
        /// <param name="callService">The service used for managing elevator calls.</param>
        public ElevatorCallController(CallService callService)
        {
            _callService = callService;
        }

        /// <summary>
        /// Creates a new elevator call from a floor.
        /// </summary>
        /// <param name="request">The call creation request.</param>
        /// <returns>The newly created elevator call.</returns>
        [HttpPost("create")]
        public IActionResult CreateCall(CreateCallRequest request)
        {
            Console.WriteLine($"📥 Received elevator call: buildingId={request.BuildingId}, floor={request.RequestedFloor}");
            var call = _callService.CreateCall(request.BuildingId, request.RequestedFloor);
            return Ok(call);
        }
    }
}
