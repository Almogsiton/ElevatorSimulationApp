using System.ComponentModel.DataAnnotations;

namespace ElevatorSimulationApi.Models.Entities;

public class User
{
    public int Id { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public virtual ICollection<Building> Buildings { get; set; } = new List<Building>();
} 