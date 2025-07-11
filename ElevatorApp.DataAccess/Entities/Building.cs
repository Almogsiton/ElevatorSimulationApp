using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents a building in the system.
    /// </summary>
    public class Building
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int NumberOfFloors { get; set; }
    }
}
