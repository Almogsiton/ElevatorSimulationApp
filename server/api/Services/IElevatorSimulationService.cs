using ElevatorSimulationApi.Models.DTOs;

namespace ElevatorSimulationApi.Services;

public interface IElevatorSimulationService
{
    Task ProcessElevatorSimulationAsync();
    Task SendElevatorUpdateAsync(int elevatorId, ElevatorUpdateMessage message);
} 