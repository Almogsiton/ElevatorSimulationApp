using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElevatorApp.DataAccess.Helpers;

namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents an elevator located in a specific building.
    /// </summary>
    public class Elevator
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Building")]
        public int BuildingId { get; set; }

        [Required]
        public int CurrentFloor { get; set; } = Constants.DefaultGroundFloor;

        [Required]
        public ElevatorStatus Status { get; set; } = ElevatorStatus.Idle;

        [Required]
        public ElevatorDirection Direction { get; set; } = ElevatorDirection.None;

        [Required]
        public DoorStatus DoorStatus { get; set; } = DoorStatus.Closed;
    }
}
