using System.ComponentModel.DataAnnotations;

namespace ElevatorApp.DataAccess.Entities
{
    /// <summary>
    /// Represents a user in the system (e.g., building manager).
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;
    }
}
