// Authentication DTOs - data transfer objects for user registration and login operations
// Contains request and response models for authentication endpoints

using System.ComponentModel.DataAnnotations;
using ElevatorSimulationApi.Config;

namespace ElevatorSimulationApi.Models.DTOs;

// User registration request with email and password validation
public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(AppConstants.Auth.MinPasswordLength)]
    public string Password { get; set; } = string.Empty;
}

// User login request with email and password
public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

// Authentication response with JWT token and user information
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int UserId { get; set; }
} 