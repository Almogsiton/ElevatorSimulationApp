// Elevator SignalR hub - manages real-time communication for elevator updates
// Handles client connections, group management, and elevator state broadcasting

using ElevatorSimulationApi.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
namespace ElevatorSimulationApi.Hubs;

public class ElevatorHub : Hub
{
    // Add client to elevator-specific group for targeted updates
    public async Task JoinElevatorGroup(int elevatorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"elevator_{elevatorId}");
    }

    // Remove client from elevator-specific group
    public async Task LeaveElevatorGroup(int elevatorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"elevator_{elevatorId}");
    }

    // Broadcast elevator update to all clients in elevator group
    public async Task ReceiveElevatorUpdate(ElevatorUpdateMessage message)
    {
        await Clients.Group($"elevator_{message.ElevatorId}").SendAsync("ReceiveElevatorUpdate", message);
    }
} 