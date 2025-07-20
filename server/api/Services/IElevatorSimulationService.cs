// Elevator simulation service interface - defines elevator simulation operations
// Handles elevator movement simulation and real-time updates

using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IElevatorSimulationService
{
    // Process elevator simulation logic and movement
    Task ProcessElevatorSimulationAsync();
    // Send real-time elevator updates via SignalR
    Task SendElevatorUpdateAsync(int elevatorId, ElevatorUpdateMessage message);
} 