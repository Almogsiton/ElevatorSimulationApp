namespace ElevatorApp.Models
{
    /// <summary>
    /// Represents a request to create a new building in the system.
    /// </summary>
    public class CreateBuildingRequest
    {
        /// <summary>
        /// Gets or sets the ID of the user who owns the building.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the name of the building.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of floors in the building.
        /// </summary>
        public int NumberOfFloors { get; set; }
    }
}
