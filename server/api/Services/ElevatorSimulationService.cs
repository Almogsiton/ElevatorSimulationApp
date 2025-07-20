// Elevator simulation service - manages real-time elevator movement, door operations, and call processing
// Handles elevator logic, target floor management, and SignalR updates to clients

using ElevatorSimulationApi.Data;
using ElevatorSimulationApi.Hubs;
using ElevatorSimulationApi.Models.DTOs;
using ElevatorSimulationApi.Models.Entities;
using ElevatorSimulationApi.Models.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ElevatorSimulationApi.Services;

public class ElevatorSimulationService : IElevatorSimulationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<ElevatorHub> _hubContext;
    private readonly ILogger<ElevatorSimulationService> _logger;
    private readonly Dictionary<int, int> _doorTimers = new();
    private readonly Dictionary<int, List<int>> _targetFloors = new();

    public ElevatorSimulationService(
        IServiceProvider serviceProvider,
        IHubContext<ElevatorHub> hubContext,
        ILogger<ElevatorSimulationService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    // Process elevator simulation for all elevators in the system
    public async Task ProcessElevatorSimulationAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                var elevators = await context.Elevators
                    .Include(e => e.Building)
                    .ToListAsync();

                // Initialize dictionaries for all elevators
                foreach (var elevator in elevators)
                {
                    if (!_doorTimers.ContainsKey(elevator.Id))
                        _doorTimers[elevator.Id] = 0;
                    if (!_targetFloors.ContainsKey(elevator.Id))
                        _targetFloors[elevator.Id] = new List<int>();
                }

                // Process all elevators
                foreach (var elevator in elevators)
                {
                    await ProcessElevatorAsync(context, elevator);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing elevator simulation");
            }
        }
    }


    // Process individual elevator operations including calls, doors, and movement
    private async Task ProcessElevatorAsync(ApplicationDbContext context, Elevator elevator)
    {
        // Initialize elevator's target floors list if first time 
        if (!_targetFloors.ContainsKey(elevator.Id))
        {
            _targetFloors[elevator.Id] = new List<int>();
        }
        
        await ProcessPendingCallsAsync(context, elevator);
        await ProcessDoorOperationsAsync(context, elevator);

        if (elevator.DoorStatus == DoorStatus.Closed)
        {
            await ProcessElevatorMovementAsync(context, elevator);
        }

        // Send update to client
        var updateMessage = new ElevatorUpdateMessage
        {
            ElevatorId = elevator.Id,
            CurrentFloor = elevator.CurrentFloor,
            Status = elevator.Status,
            Direction = elevator.Direction,
            DoorStatus = elevator.DoorStatus
        };

        await SendElevatorUpdateAsync(elevator.Id, updateMessage);
    }

    // Process pending elevator calls and determine movement direction
    private async Task ProcessPendingCallsAsync(ApplicationDbContext context, Elevator elevator)
    {
        var pendingCalls = await context.ElevatorCalls
            .Where(c => c.BuildingId == elevator.BuildingId && !c.IsHandled)
            .OrderBy(c => c.CallTime)
            .ToListAsync();

        if (pendingCalls.Count == 0)
            return;

        // If idle, determine direction by first call
        if (elevator.Status == ElevatorStatus.Idle)
        {
            var firstCall = pendingCalls.First();
            if (firstCall.RequestedFloor > elevator.CurrentFloor)
            {
                elevator.Status = ElevatorStatus.MovingUp;
                elevator.Direction = ElevatorDirection.Up;
            }
            else if (firstCall.RequestedFloor < elevator.CurrentFloor)
            {
                elevator.Status = ElevatorStatus.MovingDown;
                elevator.Direction = ElevatorDirection.Down;
            }
            else // Same floor
            {
                // Prefer Up if not top floor, else Down
                if (firstCall.RequestedFloor < elevator.Building.NumberOfFloors - 1)
                {
                    elevator.Status = ElevatorStatus.MovingUp;
                    elevator.Direction = ElevatorDirection.Up;
                }
                else
                {
                    elevator.Status = ElevatorStatus.MovingDown;
                    elevator.Direction = ElevatorDirection.Down;
                }
            }
            await context.SaveChangesAsync();
        }

        // Handle all calls - elevator should stop at any floor with a call
        foreach (var call in pendingCalls)
        {
            // Always add the requested floor if it's not the current floor
            if (call.RequestedFloor != elevator.CurrentFloor)
            {
                await AddFloorToTargetsAsync(context, elevator, call.RequestedFloor);
            }
            
            // Add destination floor if it exists and is not the current floor
            if (call.DestinationFloor.HasValue && call.DestinationFloor.Value != elevator.CurrentFloor)
            {
                await AddFloorToTargetsAsync(context, elevator, call.DestinationFloor.Value);
            }
        }
    }

    // Process elevator movement logic and floor transitions
    private async Task ProcessElevatorMovementAsync(ApplicationDbContext context, Elevator elevator)
    {
        if (elevator.DoorStatus != DoorStatus.Closed)
        {
            return;
        }

        // Floor boundary protection
        int maxFloor = elevator.Building.NumberOfFloors - 1;
        if (elevator.CurrentFloor < 0) elevator.CurrentFloor = 0;
        if (elevator.CurrentFloor > maxFloor) elevator.CurrentFloor = maxFloor;

        // If idle and has targets, start movement
        if (elevator.Status == ElevatorStatus.Idle && _targetFloors[elevator.Id].Any())
        {
            var nextFloor = _targetFloors[elevator.Id].First();
            if (nextFloor > elevator.CurrentFloor)
            {
                elevator.Status = ElevatorStatus.MovingUp;
                elevator.Direction = ElevatorDirection.Up;
            }
            else if (nextFloor < elevator.CurrentFloor)
            {
                elevator.Status = ElevatorStatus.MovingDown;
                elevator.Direction = ElevatorDirection.Down;
            }
            await context.SaveChangesAsync();
        }
        else if (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown)
        {
            if (_targetFloors[elevator.Id].Contains(elevator.CurrentFloor))
            {
                elevator.Status = ElevatorStatus.OpeningDoors;
                // אל תשנה את הכיוון כאן!
                _doorTimers[elevator.Id] = 0;
                _targetFloors[elevator.Id].Remove(elevator.CurrentFloor);
                await context.SaveChangesAsync();
            }
            else
            {
                if (elevator.Status == ElevatorStatus.MovingUp)
                    elevator.CurrentFloor++;
                else
                    elevator.CurrentFloor--;

                // הגנה על חריגה
                if (elevator.CurrentFloor < 0) elevator.CurrentFloor = 0;
                if (elevator.CurrentFloor > maxFloor) elevator.CurrentFloor = maxFloor;

                await context.SaveChangesAsync();
            }
        }
    }


    private async Task ProcessDoorOperationsAsync(ApplicationDbContext context, Elevator elevator)
    {
        // Opening doors
        if (elevator.Status == ElevatorStatus.OpeningDoors)
        {
            if (!_doorTimers.ContainsKey(elevator.Id))
                _doorTimers[elevator.Id] = 0;

            _doorTimers[elevator.Id]++;
            elevator.DoorStatus = DoorStatus.Opening;

            if (_doorTimers[elevator.Id] >= 2) // Opening time
            {
                elevator.DoorStatus = DoorStatus.Open;
                elevator.Status = ElevatorStatus.Idle;
                _doorTimers[elevator.Id] = 0;

                // Mark relevant calls as handled
                var callsToHandle = await context.ElevatorCalls
                    .Where(c => c.BuildingId == elevator.BuildingId && !c.IsHandled &&
                        (c.RequestedFloor == elevator.CurrentFloor || c.DestinationFloor == elevator.CurrentFloor))
                    .ToListAsync();

                foreach (var call in callsToHandle)
                {
                    await MarkCallAsHandledAsync(context, call);
                }

                await context.SaveChangesAsync();

                // Reset direction if no targets
                if (!_targetFloors[elevator.Id].Any())
                {
                    elevator.Direction = ElevatorDirection.None;
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                await context.SaveChangesAsync();
            }
            return;
        }

        // Waiting for user target (doors are open)
        if (elevator.DoorStatus == DoorStatus.Open)
        {
            return;
        }

        // Closing doors
        if (elevator.Status == ElevatorStatus.ClosingDoors)
        {
            if (!_doorTimers.ContainsKey(elevator.Id))
                _doorTimers[elevator.Id] = 0;

            _doorTimers[elevator.Id]++;
            elevator.DoorStatus = DoorStatus.Closing;

            if (_doorTimers[elevator.Id] >= 2) // Closing time
            {
                elevator.DoorStatus = DoorStatus.Closed;
                if (_targetFloors[elevator.Id].Any())
                {
                    var nextFloor = _targetFloors[elevator.Id].First();
                    if (nextFloor > elevator.CurrentFloor)
                    {
                        elevator.Status = ElevatorStatus.MovingUp;
                        elevator.Direction = ElevatorDirection.Up;
                    }
                    else if (nextFloor < elevator.CurrentFloor)
                    {
                        elevator.Status = ElevatorStatus.MovingDown;
                        elevator.Direction = ElevatorDirection.Down;
                    }
                }
                else
                {
                    elevator.Status = ElevatorStatus.Idle;
                    elevator.Direction = ElevatorDirection.None;
                }
                _doorTimers.Remove(elevator.Id);
                await context.SaveChangesAsync();
            }
            else
            {
                await context.SaveChangesAsync();
            }
            return;
        }
    }


    private bool IsCallOnTheWay(Elevator elevator, ElevatorCall call)
    {
        if (elevator.Status == ElevatorStatus.MovingUp && elevator.Direction == ElevatorDirection.Up)
        {
            return call.RequestedFloor >= elevator.CurrentFloor;
        }
        else if (elevator.Status == ElevatorStatus.MovingDown && elevator.Direction == ElevatorDirection.Down)
        {
            return call.RequestedFloor <= elevator.CurrentFloor;
        }
        return false;
    }

    private async Task AssignCallToElevatorAsync(ApplicationDbContext context, Elevator elevator, ElevatorCall call)
    {
        await AddFloorToTargetsAsync(context, elevator, call.RequestedFloor);
        if (call.DestinationFloor.HasValue)
        {
            await AddFloorToTargetsAsync(context, elevator, call.DestinationFloor.Value);
        }

        var assignment = new ElevatorCallAssignment
        {
            ElevatorId = elevator.Id,
            ElevatorCallId = call.Id,
            AssignmentTime = DateTime.UtcNow
        };
        context.ElevatorCallAssignments.Add(assignment);
    }

    private async Task AddFloorToTargetsAsync(ApplicationDbContext context, Elevator elevator, int floor)
    {
        if (!_targetFloors[elevator.Id].Contains(floor))
        {
            _targetFloors[elevator.Id].Add(floor);
            _targetFloors[elevator.Id].Sort();
        }

        // If doors are open, begin closing them
        if (elevator.DoorStatus == DoorStatus.Open)
        {
            elevator.Status = ElevatorStatus.ClosingDoors;
            elevator.DoorStatus = DoorStatus.Closing;
            _doorTimers[elevator.Id] = 0;
            await context.SaveChangesAsync();
        }
    }

    private async Task MarkCallAsHandledAsync(ApplicationDbContext context, ElevatorCall call)
    {
        call.IsHandled = true;
        await context.SaveChangesAsync();
    }

    public async Task SendElevatorUpdateAsync(int elevatorId, ElevatorUpdateMessage message)
    {
        if (message == null)
        {
            _logger.LogError("ElevatorUpdateMessage is null for elevator {ElevatorId}", elevatorId);
            return;
        }
        
        await _hubContext.Clients.Group($"elevator_{elevatorId}")
            .SendAsync("ReceiveElevatorUpdate", message);
    }
}
