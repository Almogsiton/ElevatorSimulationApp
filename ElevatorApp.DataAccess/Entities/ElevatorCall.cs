using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents a request for an elevator to pick up or deliver a passenger.
    /// </summary>
    public class ElevatorCall
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Building")]
        public int BuildingId { get; set; }

        [Required]
        public int RequestedFloor { get; set; }

        public int? DestinationFloor { get; set; }

        [Required]
        public DateTime CallTime { get; set; }

        [Required]
        public bool IsHandled { get; set; }
    }
}
