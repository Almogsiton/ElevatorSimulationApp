using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<bool> ValidateTokenAsync(string token);
    Task<int> GetUserIdFromTokenAsync(string token);
} 