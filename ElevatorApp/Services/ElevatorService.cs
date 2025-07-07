using ElevatorApp.DataAccess.Context;
using ElevatorApp.DataAccess.Entities;

namespace ElevatorApp.Services
{
    /// <summary>
    /// Provides operations for retrieving elevator status information.
    /// </summary>
    public class ElevatorService
    {
        private readonly ElevatorDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElevatorService"/> class.
        /// </summary>
        /// <param name="context">The database context used to access elevators.</param>
        public ElevatorService(ElevatorDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves the elevator by ID.
        /// </summary>
        /// <param name="elevatorId">The ID of the elevator.</param>
        /// <returns>The elevator entity, or null if not found.</returns>
        public Elevator? GetElevatorStatus(int elevatorId)
        {
            return _context.Elevators.FirstOrDefault(e => e.Id == elevatorId);
        }
    }
}
