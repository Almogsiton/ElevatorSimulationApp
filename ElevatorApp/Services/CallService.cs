using ElevatorApp.DataAccess.Context;
using ElevatorApp.DataAccess.Entities;

namespace ElevatorApp.Services
{
    /// <summary>
    /// Provides operations for creating elevator calls.
    /// </summary>
    public class CallService
    {
        private readonly ElevatorDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallService"/> class.
        /// </summary>
        /// <param name="context">The database context used to manage calls.</param>
        public CallService(ElevatorDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new elevator call for pickup.
        /// </summary>
        /// <param name="buildingId">The ID of the building.</param>
        /// <param name="requestedFloor">The floor where the elevator is requested.</param>
        /// <returns>The created call entity.</returns>
        public ElevatorCall CreateCall(int buildingId, int requestedFloor)
        {
            var call = new ElevatorCall
            {
                BuildingId = buildingId,
                RequestedFloor = requestedFloor,
                CallTime = DateTime.Now,
                IsHandled = false
            };

            _context.ElevatorCalls.Add(call);
            _context.SaveChanges();

            return call;
        }
    }
}
