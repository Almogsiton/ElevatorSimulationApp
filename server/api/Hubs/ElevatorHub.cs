using ElevatorSimulationApi.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
namespace ElevatorSimulationApi.Hubs;


/// <summary>
///  todo remove uneeded funcs
/// </summary>
public class ElevatorHub : Hub
{
    public async Task JoinElevatorGroup(int elevatorId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"elevator_{elevatorId}");
    }

    public async Task LeaveElevatorGroup(int elevatorId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"elevator_{elevatorId}");
    }

    public async Task ReceiveElevatorUpdate(ElevatorUpdateMessage message)
    {
        await Clients.Group($"elevator_{message.ElevatorId}").SendAsync("ReceiveElevatorUpdate", message);
    }
} 