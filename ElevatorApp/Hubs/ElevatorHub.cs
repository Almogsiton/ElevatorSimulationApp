using Microsoft.AspNetCore.SignalR;

namespace ElevatorApp.Hubs
{
    /// <summary>
    /// SignalR hub for sending elevator status updates to connected clients in real time.
    /// </summary>
    public class ElevatorHub : Hub
    {
        // At this stage, no methods are required from the client.
        // The server will push updates using Clients.All.SendAsync(...)
    }
}
