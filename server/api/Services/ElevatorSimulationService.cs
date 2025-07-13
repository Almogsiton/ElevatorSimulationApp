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
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<ElevatorHub> _hubContext;
    private readonly ILogger<ElevatorSimulationService> _logger;
    private readonly Dictionary<int, int> _doorTimers = new();
    private readonly Dictionary<int, List<int>> _targetFloors = new();

    public ElevatorSimulationService(
        ApplicationDbContext context,
        IHubContext<ElevatorHub> hubContext,
        ILogger<ElevatorSimulationService> logger)
    {
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task ProcessElevatorSimulationAsync()
    {
        try
        {
            var elevators = await _context.Elevators
                .Include(e => e.Building)
                .ToListAsync();

            Console.WriteLine($"Processing {elevators.Count} elevators");
            
            foreach (var elevator in elevators)
            {
                Console.WriteLine($"Processing elevator {elevator.Id} at floor {elevator.CurrentFloor}, status: {elevator.Status}");
                await ProcessElevatorAsync(elevator);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing elevator simulation");
        }
    }

    private async Task ProcessElevatorAsync(Elevator elevator)
    {
        if (!_targetFloors.ContainsKey(elevator.Id))
        {
            _targetFloors[elevator.Id] = new List<int>();
        }

        await ProcessPendingCallsAsync(elevator);
        await ProcessElevatorMovementAsync(elevator);
        await ProcessDoorOperationsAsync(elevator);
        var updateMessage = new ElevatorUpdateMessage
        {
            ElevatorId = elevator.Id,
            CurrentFloor = elevator.CurrentFloor,
            Status = elevator.Status,
            Direction = elevator.Direction,
            DoorStatus = elevator.DoorStatus
        };
        
        Console.WriteLine($"Sending elevator update: Floor {updateMessage.CurrentFloor}, Status {updateMessage.Status}, Direction {updateMessage.Direction}, Door {updateMessage.DoorStatus}");
        await SendElevatorUpdateAsync(elevator.Id, updateMessage);
    }

    private async Task ProcessPendingCallsAsync(Elevator elevator)
    {
        var pendingCalls = await _context.ElevatorCalls
            .Where(c => c.BuildingId == elevator.BuildingId && !c.IsHandled)
            .OrderBy(c => c.CallTime)
            .ToListAsync();

        Console.WriteLine($"Found {pendingCalls.Count} pending calls for elevator {elevator.Id}");

        foreach (var call in pendingCalls)
        {
            Console.WriteLine($"Processing call {call.Id} for floor {call.RequestedFloor}");
            if (elevator.Status == ElevatorStatus.Idle)
            {
                Console.WriteLine($"Assigning call {call.Id} to idle elevator {elevator.Id}");
                await AssignCallToElevatorAsync(elevator, call);
            }
            else if (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown)
            {
                if (IsCallOnTheWay(elevator, call))
                {
                    Console.WriteLine($"Adding call {call.Id} to elevator {elevator.Id} on the way");
                    await AddFloorToTargetsAsync(elevator, call.RequestedFloor);
                    if (call.DestinationFloor.HasValue)
                    {
                        await AddFloorToTargetsAsync(elevator, call.DestinationFloor.Value);
                    }
                    await MarkCallAsHandledAsync(call);
                }
            }
        }
    }

    private async Task ProcessElevatorMovementAsync(Elevator elevator)
    {
        // Only allow movement if doors are fully closed
        if (elevator.DoorStatus != DoorStatus.Closed)
        {
            // Doors are not closed, do not move
            return;
        }
        if (elevator.Status == ElevatorStatus.Idle && _targetFloors[elevator.Id].Any())
        {
            var nextFloor = _targetFloors[elevator.Id].First();
            Console.WriteLine($"Elevator {elevator.Id} starting to move to floor {nextFloor}");
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
            await _context.SaveChangesAsync();
        }
        else if (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown)
        {
            if (_targetFloors[elevator.Id].Contains(elevator.CurrentFloor))
            {
                Console.WriteLine($"Elevator {elevator.Id} arrived at floor {elevator.CurrentFloor}, opening doors and waiting for user input");
                elevator.Status = ElevatorStatus.OpeningDoors;
                elevator.Direction = ElevatorDirection.None;
                _doorTimers[elevator.Id] = 0;
                _targetFloors[elevator.Id].Remove(elevator.CurrentFloor);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (elevator.Status == ElevatorStatus.MovingUp)
                {
                    elevator.CurrentFloor++;
                }
                else
                {
                    elevator.CurrentFloor--;
                }
                Console.WriteLine($"Elevator {elevator.Id} moved to floor {elevator.CurrentFloor}");
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task ProcessDoorOperationsAsync(Elevator elevator)
    {
        if (elevator.Status == ElevatorStatus.OpeningDoors)
        {
            if (!_doorTimers.ContainsKey(elevator.Id))
            {
                _doorTimers[elevator.Id] = 0;
            }

            _doorTimers[elevator.Id]++;
            elevator.DoorStatus = DoorStatus.Opening;

            if (_doorTimers[elevator.Id] >= 3)
            {
                elevator.DoorStatus = DoorStatus.Open;
                await _context.SaveChangesAsync();
                // Now WAIT for user input (destination selection) before closing doors
            }
        }
        else if (elevator.Status == ElevatorStatus.ClosingDoors)
        {
            _doorTimers[elevator.Id]++;
            elevator.DoorStatus = DoorStatus.Closing;

            if (_doorTimers[elevator.Id] >= 3)
            {
                // After doors close, determine next movement or idle
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
                    _targetFloors[elevator.Id].Remove(nextFloor);
                }
                else
                {
                    elevator.Status = ElevatorStatus.Idle;
                    elevator.Direction = ElevatorDirection.None;
                }
                elevator.DoorStatus = DoorStatus.Closed;
                _doorTimers.Remove(elevator.Id);
                await _context.SaveChangesAsync();
            }
        }
        else if (elevator.DoorStatus == DoorStatus.Open)
        {
            // Doors are open and waiting for user input, do nothing
            return;
        }
        else
        {
            elevator.DoorStatus = DoorStatus.Closed;
            await _context.SaveChangesAsync();
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

    private async Task AssignCallToElevatorAsync(Elevator elevator, ElevatorCall call)
    {
        await AddFloorToTargetsAsync(elevator, call.RequestedFloor);
        if (call.DestinationFloor.HasValue)
        {
            await AddFloorToTargetsAsync(elevator, call.DestinationFloor.Value);
        }

        var assignment = new ElevatorCallAssignment
        {
            ElevatorId = elevator.Id,
            ElevatorCallId = call.Id,
            AssignmentTime = DateTime.UtcNow
        };

        _context.ElevatorCallAssignments.Add(assignment);
        await MarkCallAsHandledAsync(call);
    }

    private async Task AddFloorToTargetsAsync(Elevator elevator, int floor)
    {
        if (!_targetFloors[elevator.Id].Contains(floor))
        {
            _targetFloors[elevator.Id].Add(floor);
            _targetFloors[elevator.Id].Sort();
        }
        // If elevator is waiting with doors open, start closing doors and resume movement
        if (elevator.DoorStatus == DoorStatus.Open)
        {
            elevator.Status = ElevatorStatus.ClosingDoors;
            elevator.DoorStatus = DoorStatus.Closing;
            _doorTimers[elevator.Id] = 0;
            await _context.SaveChangesAsync();
        }
    }

    private async Task MarkCallAsHandledAsync(ElevatorCall call)
    {
        call.IsHandled = true;
        await _context.SaveChangesAsync();
    }

    public async Task SendElevatorUpdateAsync(int elevatorId, ElevatorUpdateMessage message)
    {
        Console.WriteLine($"Sending SignalR update to group elevator_{elevatorId}");
        await _hubContext.Clients.Group($"elevator_{elevatorId}").SendAsync("ReceiveElevatorUpdate", message);
    }
} 