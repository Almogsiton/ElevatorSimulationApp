using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents the assignment of an elevator to a specific elevator call.
    /// </summary>
    public class ElevatorCallAssignment
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Elevator")]
        public int ElevatorId { get; set; }

        [Required]
        public DateTime AssignmentTime { get; set; }
    }
}
