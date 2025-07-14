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


            // start proccesing the elevators 
            foreach (var elevator in elevators)
            {
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
        // init elevator's target floors lists if first time 
        if (!_targetFloors.ContainsKey(elevator.Id))
        {
            _targetFloors[elevator.Id] = new List<int>();
        }

        await ProcessPendingCallsAsync(elevator); // Assign pending calls to this elevator (from the database). - if idle the request will be added to target floor 
        await ProcessElevatorMovementAsync(elevator); // Moves the elevator one floor at a time toward the next target floor (if doors are closed), update status and direction 
        await ProcessDoorOperationsAsync(elevator); // Handles opening, closing, and waiting (dors)
        
        // create update message object and send it to the client 
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

    private async Task ProcessPendingCallsAsync(Elevator elevator)
    {
        var pendingCalls = await _context.ElevatorCalls
            .Where(c => c.BuildingId == elevator.BuildingId && !c.IsHandled)
            .OrderBy(c => c.CallTime)
            .ToListAsync();


        foreach (var call in pendingCalls)
        {
            // idle - watiting
            if (elevator.Status == ElevatorStatus.Idle)
            {
                await AssignCallToElevatorAsync(elevator, call);
            }
            else if (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown)
            {
                if (IsCallOnTheWay(elevator, call))
                {
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
    // dont move if the 
    if (elevator.DoorStatus != DoorStatus.Closed)
    {
        Console.WriteLine($"🛑 Elevator {elevator.Id} לא זזה כי הדלת במצב: {elevator.DoorStatus}");
        return;
    }

    // הגנה על חריגה מהקומות
    int maxFloor = elevator.Building.NumberOfFloors - 1;
    if (elevator.CurrentFloor < 0) elevator.CurrentFloor = 0;
    if (elevator.CurrentFloor > maxFloor) elevator.CurrentFloor = maxFloor;

    // אם יש יעד והמעלית ב-Idle, התחל תנועה
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
        await _context.SaveChangesAsync();
    }
    else if (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown)
    {
        if (_targetFloors[elevator.Id].Contains(elevator.CurrentFloor))
        {
            elevator.Status = ElevatorStatus.OpeningDoors;
            elevator.Direction = ElevatorDirection.None;
            _doorTimers[elevator.Id] = 0;
            _targetFloors[elevator.Id].Remove(elevator.CurrentFloor);
            await _context.SaveChangesAsync();
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

            await _context.SaveChangesAsync();
        }
    }
}

   private async Task ProcessDoorOperationsAsync(Elevator elevator)
{
    // פתיחת דלתות
    if (elevator.Status == ElevatorStatus.OpeningDoors)
    {
        if (!_doorTimers.ContainsKey(elevator.Id))
            _doorTimers[elevator.Id] = 0;

        _doorTimers[elevator.Id]++;
        elevator.DoorStatus = DoorStatus.Opening;

        if (_doorTimers[elevator.Id] >= 2) // זמן פתיחה
        {
            elevator.DoorStatus = DoorStatus.Open;
            elevator.Status = ElevatorStatus.Idle; // תמיד עובר ל-Idle כשהדלתות פתוחות
            _doorTimers[elevator.Id] = 0;
            await _context.SaveChangesAsync();
        }
        else
        {
            await _context.SaveChangesAsync();
        }
        return;
    }

    // המתנה ליעד מהמשתמש (הדלתות פתוחות)
    if (elevator.DoorStatus == DoorStatus.Open)
    {
        // לא עושים כלום עד שמתווסף יעד חדש
        return;
    }

    // סגירת דלתות
    if (elevator.Status == ElevatorStatus.ClosingDoors)
    {
        if (!_doorTimers.ContainsKey(elevator.Id))
            _doorTimers[elevator.Id] = 0;

        _doorTimers[elevator.Id]++;
        elevator.DoorStatus = DoorStatus.Closing;

        if (_doorTimers[elevator.Id] >= 2) // זמן סגירה
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
            await _context.SaveChangesAsync();
        }
        else
        {
            await _context.SaveChangesAsync();
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
    
    // אם הדלתות פתוחות, התחל לסגור אותן
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