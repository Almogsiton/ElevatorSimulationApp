using ElevatorApp.DataAccess.Context;
using ElevatorApp.DataAccess.Entities;

namespace ElevatorApp.Services
{
    /// <summary>
    /// Provides operations related to buildings, including creation and retrieval.
    /// </summary>
    public class BuildingService
    {
        private readonly ElevatorDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildingService"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing buildings.</param>
        public BuildingService(ElevatorDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns a list of buildings owned by the given user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>A list of buildings.</returns>
        public List<Building> GetBuildingsByUser(int userId)
        {
            return _context.Buildings
                .Where(b => b.UserId == userId)
                .ToList();
        }

        /// <summary>
        /// Creates a new building and associates it with the user. Automatically adds one elevator.
        /// </summary>
        /// <param name="userId">The ID of the user creating the building.</param>
        /// <param name="name">The name of the building.</param>
        /// <param name="floors">The number of floors in the building.</param>
        /// <returns>The created building.</returns>
        public Building CreateBuilding(int userId, string name, int floors)
        {
            var building = new Building
            {
                UserId = userId,
                Name = name,
                NumberOfFloors = floors
            };

            _context.Buildings.Add(building);
            _context.SaveChanges();

            // Automatically create elevator for the building
            var elevator = new Elevator
            {
                BuildingId = building.Id
            };

            _context.Elevators.Add(elevator);
            _context.SaveChanges();

            return building;
        }
    }
}
