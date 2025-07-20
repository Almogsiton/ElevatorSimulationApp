// Authentication service interface - defines user authentication and authorization methods
// Handles user registration, login, token validation, and user identification

using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IAuthService
{
    // Register new user and return authentication response
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    // Authenticate user and return JWT token
    Task<AuthResponse> LoginAsync(LoginRequest request);
    // Validate JWT token authenticity
    Task<bool> ValidateTokenAsync(string token);
    // Extract user ID from JWT token
    Task<int> GetUserIdFromTokenAsync(string token);
} 