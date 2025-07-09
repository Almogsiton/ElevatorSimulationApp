namespace ElevatorApp.Models
{
    /// <summary>
    /// Represents a request to create an elevator call from a floor.
    /// </summary>
    public class CreateCallRequest
    {
        public int BuildingId { get; set; }
        public int RequestedFloor { get; set; }

        public string Direction { get; set; }
    }
}
