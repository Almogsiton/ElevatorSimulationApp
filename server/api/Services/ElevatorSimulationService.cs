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

    public async Task ProcessElevatorSimulationAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                _logger.LogInformation("1.🔄 Starting to load elevators from the database...");
                var elevators = await context.Elevators
                    .Include(e => e.Building)
                    .ToListAsync();
                _logger.LogInformation("1.✅ Finished loading elevators from the database. Found {Count} elevators.", elevators.Count);

                // Initialize dictionaries for all elevators
                foreach (var elevator in elevators)
                {
                    if (!_doorTimers.ContainsKey(elevator.Id))
                        _doorTimers[elevator.Id] = 0;
                    if (!_targetFloors.ContainsKey(elevator.Id))
                        _targetFloors[elevator.Id] = new List<int>();
                }

                // start proccesing the elevators 
                foreach (var elevator in elevators)
                {
                    _logger.LogInformation("2.➡️ Starting processing elevator ID: {ElevatorId}", elevator.Id);
                    await ProcessElevatorAsync(context, elevator);
                    _logger.LogInformation("2.✔️ Finished processing elevator ID: {ElevatorId}", elevator.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing elevator simulation");
            }
        }
    }


    private async Task ProcessElevatorAsync(ApplicationDbContext context, Elevator elevator)
    {
        // init elevator's target floors lists if first time 
        if (!_targetFloors.ContainsKey(elevator.Id))
        {
            _targetFloors[elevator.Id] = new List<int>();
        }
        _logger.LogInformation("3.🔽 Start: Processing pending calls for elevator {ElevatorId}", elevator.Id);
        await ProcessPendingCallsAsync(context, elevator); // Assign pending calls to this elevator (from the database). - if idle the request will be added to target floor 
        _logger.LogInformation("3.✅ Done: Pending calls processed for elevator {ElevatorId}", elevator.Id);

        _logger.LogInformation("4.🚪 Start: Processing door operations for elevator {ElevatorId}", elevator.Id);
        await ProcessDoorOperationsAsync(context, elevator); // Handles opening, closing, and waiting (dors)
        _logger.LogInformation("4.✅ Done: Door operations processed for elevator {ElevatorId}", elevator.Id);

        if (elevator.DoorStatus != DoorStatus.Closed)
        {
            _logger.LogWarning("🚫 Skipping movement for elevator {ElevatorId} because doors are {DoorStatus}", elevator.Id, elevator.DoorStatus);
        }
        else
        {
            _logger.LogInformation("5.🔼 Start: Moving elevator {ElevatorId}", elevator.Id);
            await ProcessElevatorMovementAsync(context, elevator);
            _logger.LogInformation("5.✅ Done: Movement processed for elevator {ElevatorId}", elevator.Id);
        }


        // create update message object and send it to the client 
        var updateMessage = new ElevatorUpdateMessage
        {
            ElevatorId = elevator.Id,
            CurrentFloor = elevator.CurrentFloor,
            Status = elevator.Status,
            Direction = elevator.Direction,
            DoorStatus = elevator.DoorStatus
        };

        _logger.LogInformation("6.📡 Start: Sending update to client for elevator {ElevatorId}", elevator.Id);
        await SendElevatorUpdateAsync(elevator.Id, updateMessage);
        _logger.LogInformation("6.✅ Done: Update sent to client for elevator {ElevatorId}", elevator.Id);
    }

    private async Task ProcessPendingCallsAsync(ApplicationDbContext context, Elevator elevator)
    {
        _logger.LogInformation("7.📥 Start: Loading pending calls for elevator {ElevatorId}", elevator.Id);
        var pendingCalls = await context.ElevatorCalls
            .Where(c => c.BuildingId == elevator.BuildingId && !c.IsHandled)
            .OrderBy(c => c.CallTime)
            .ToListAsync();
        _logger.LogInformation("7.✅ Done: Loaded {Count} pending calls for elevator {ElevatorId}", pendingCalls.Count, elevator.Id);

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

        // Only handle calls matching direction
        foreach (var call in pendingCalls)
        {
            if (elevator.Status == ElevatorStatus.MovingUp && call.RequestedFloor > elevator.CurrentFloor)
            {
                await AddFloorToTargetsAsync(context, elevator, call.RequestedFloor);
                if (call.DestinationFloor.HasValue)
                    await AddFloorToTargetsAsync(context, elevator, call.DestinationFloor.Value);
            }
            else if (elevator.Status == ElevatorStatus.MovingDown && call.RequestedFloor < elevator.CurrentFloor)
            {
                await AddFloorToTargetsAsync(context, elevator, call.RequestedFloor);
                if (call.DestinationFloor.HasValue)
                    await AddFloorToTargetsAsync(context, elevator, call.DestinationFloor.Value);
            }
        }
    }

    private async Task ProcessElevatorMovementAsync(ApplicationDbContext context, Elevator elevator)
    {
        _logger.LogInformation("🚦 Start: Processing movement for elevator {ElevatorId}", elevator.Id);
        // dont move if the 
        if (elevator.DoorStatus != DoorStatus.Closed)
        {
            _logger.LogWarning("🚫 Elevator {ElevatorId} cannot move because the doors are {DoorStatus}", elevator.Id, elevator.DoorStatus);
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
            _logger.LogInformation("12.💾 Saving movement start status for elevator {ElevatorId}", elevator.Id);
            await context.SaveChangesAsync();
            _logger.LogInformation("12.✅ Movement start status saved for elevator {ElevatorId}", elevator.Id);

        }
        else if (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown)
        {
            if (_targetFloors[elevator.Id].Contains(elevator.CurrentFloor))
            {
                elevator.Status = ElevatorStatus.OpeningDoors;
                // אל תשנה את הכיוון כאן!
                _doorTimers[elevator.Id] = 0;
                _targetFloors[elevator.Id].Remove(elevator.CurrentFloor);

                _logger.LogInformation("13🚪 Elevator {ElevatorId} reached target floor {Floor} and is opening doors", elevator.Id, elevator.CurrentFloor);
                await context.SaveChangesAsync();
                _logger.LogInformation("13✅ Door opening status saved for elevator {ElevatorId}", elevator.Id);
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

                _logger.LogInformation("14.⬆️⬇️ Elevator {ElevatorId} moved to floor {Floor}", elevator.Id, elevator.CurrentFloor);
                await context.SaveChangesAsync();
                _logger.LogInformation("14.✅ Floor update saved for elevator {ElevatorId}", elevator.Id);
            }
        }
    }


    private async Task ProcessDoorOperationsAsync(ApplicationDbContext context, Elevator elevator)
    {
        _logger.LogInformation($"[DOOR] Start: Elevator {elevator.Id} | Status: {elevator.Status} | DoorStatus: {elevator.DoorStatus} | Floor: {elevator.CurrentFloor} | Timer: {_doorTimers.GetValueOrDefault(elevator.Id, -1)}");
        // פתיחת דלתות
        if (elevator.Status == ElevatorStatus.OpeningDoors)
        {
            if (!_doorTimers.ContainsKey(elevator.Id))
                _doorTimers[elevator.Id] = 0;

            _logger.LogInformation($"[DOOR] Opening: Elevator {elevator.Id} | Progress: {_doorTimers[elevator.Id]}");
            _doorTimers[elevator.Id]++;
            elevator.DoorStatus = DoorStatus.Opening;

            if (_doorTimers[elevator.Id] >= 2) // זמן פתיחה
            {
                elevator.DoorStatus = DoorStatus.Open;
                elevator.Status = ElevatorStatus.Idle; // תמיד עובר ל-Idle כשהדלתות פתוחות
                _logger.LogInformation($"[DOOR] Doors are now OPEN! Elevator {elevator.Id} | Floor: {elevator.CurrentFloor} | Resetting timer");
                _doorTimers[elevator.Id] = 0;

                // סמן כל קריאה רלוונטית כבוצעה (requestedFloor או destinationFloor)
                var callsToHandle = await context.ElevatorCalls
                    .Where(c => c.BuildingId == elevator.BuildingId && !c.IsHandled &&
                        (c.RequestedFloor == elevator.CurrentFloor || c.DestinationFloor == elevator.CurrentFloor))
                    .ToListAsync();

                foreach (var call in callsToHandle)
                {
                    _logger.LogInformation($"[HANDLED] Marking call {call.Id} as handled at floor {elevator.CurrentFloor}");
                    await MarkCallAsHandledAsync(context, call);
                }
                if (callsToHandle.Count > 0)
                {
                    _logger.LogInformation($"[DOOR] Marked {callsToHandle.Count} calls as handled at floor {elevator.CurrentFloor} for elevator {elevator.Id}");
                }

                _logger.LogInformation($"[DOOR] Saving door open state for elevator {elevator.Id}");
                await context.SaveChangesAsync();
                _logger.LogInformation($"[DOOR] Door open state saved for elevator {elevator.Id}");

                // הוסף כאן איפוס כיוון אם אין יעדים
                if (!_targetFloors[elevator.Id].Any())
                {
                    elevator.Direction = ElevatorDirection.None;
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"[DOOR] No more targets. Direction set to None for elevator {elevator.Id}");
                }
            }
            else
            {
                _logger.LogInformation($"[DOOR] Doors still opening for elevator {elevator.Id}, progress: {_doorTimers[elevator.Id]}");
                await context.SaveChangesAsync();
                _logger.LogInformation($"[DOOR] Intermediate opening state saved for elevator {elevator.Id}");
            }
            return;
        }

        // המתנה ליעד מהמשתמש (הדלתות פתוחות)
        if (elevator.DoorStatus == DoorStatus.Open)
        {
            _logger.LogInformation($"[DOOR] Doors are OPEN and waiting for target. Elevator {elevator.Id} | Floor: {elevator.CurrentFloor}");
            // לא עושים כלום עד שמתווסף יעד חדש
            return;
        }

        // סגירת דלתות
        if (elevator.Status == ElevatorStatus.ClosingDoors)
        {
            if (!_doorTimers.ContainsKey(elevator.Id))
                _doorTimers[elevator.Id] = 0;

            _logger.LogInformation($"[DOOR] Closing: Elevator {elevator.Id} | Progress: {_doorTimers[elevator.Id]}");
            _doorTimers[elevator.Id]++;
            elevator.DoorStatus = DoorStatus.Closing;

            if (_doorTimers[elevator.Id] >= 2) // זמן סגירה
            {
                elevator.DoorStatus = DoorStatus.Closed;
                _logger.LogInformation($"[DOOR] Doors are now CLOSED! Elevator {elevator.Id} | Floor: {elevator.CurrentFloor} | Resetting timer");
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
                _logger.LogInformation($"[DOOR] Resetting timer for elevator {elevator.Id}");
                _doorTimers.Remove(elevator.Id);

                _logger.LogInformation($"[DOOR] Saving door closed state for elevator {elevator.Id}");
                await context.SaveChangesAsync();
                _logger.LogInformation($"[DOOR] Door closed state saved for elevator {elevator.Id}");
            }
            else
            {
                _logger.LogInformation($"[DOOR] Doors still closing for elevator {elevator.Id}, progress: {_doorTimers[elevator.Id]}");
                await context.SaveChangesAsync();
                _logger.LogInformation($"[DOOR] Intermediate closing state saved for elevator {elevator.Id}");
            }
            return;
        }
        _logger.LogInformation($"[DOOR] End: Elevator {elevator.Id} | Status: {elevator.Status} | DoorStatus: {elevator.DoorStatus} | Floor: {elevator.CurrentFloor} | Timer: {_doorTimers.GetValueOrDefault(elevator.Id, -1)}");
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
        _logger.LogInformation("📥 Assigning call {CallId} to elevator {ElevatorId}", call.Id, elevator.Id);

        _logger.LogInformation("19.➕ Adding requested floor {Floor} to targets", call.RequestedFloor);
        await AddFloorToTargetsAsync(context, elevator, call.RequestedFloor);
        _logger.LogInformation("19.✅ Requested floor {Floor} added to targets", call.RequestedFloor);
        if (call.DestinationFloor.HasValue)
        {
            _logger.LogInformation("20.➕ Adding destination floor {Floor} to targets", call.DestinationFloor.Value);
            await AddFloorToTargetsAsync(context, elevator, call.DestinationFloor.Value);
            _logger.LogInformation("20.✅ Destination floor {Floor} added to targets", call.DestinationFloor.Value);
        }

        var assignment = new ElevatorCallAssignment
        {
            ElevatorId = elevator.Id,
            ElevatorCallId = call.Id,
            AssignmentTime = DateTime.UtcNow
        };
        _logger.LogInformation("📝 Creating elevator-call assignment record");
        context.ElevatorCallAssignments.Add(assignment);

        // הסר את MarkCallAsHandledAsync כאן
    }

    private async Task AddFloorToTargetsAsync(ApplicationDbContext context, Elevator elevator, int floor)
    {
        if (!_targetFloors[elevator.Id].Contains(floor))
        {
            _logger.LogInformation("➕ Adding floor {Floor} to elevator {ElevatorId} target list", floor, elevator.Id);
            _targetFloors[elevator.Id].Add(floor);
            _targetFloors[elevator.Id].Sort();
            _logger.LogInformation("✅ Floor {Floor} added and targets sorted for elevator {ElevatorId}", floor, elevator.Id);
        }
        else
        {
            _logger.LogInformation("ℹ️ Floor {Floor} is already in target list for elevator {ElevatorId}", floor, elevator.Id);
        }

        // If doors are open, begin closing them
        if (elevator.DoorStatus == DoorStatus.Open)
        {
            _logger.LogInformation("🚪 Doors are open on elevator {ElevatorId} – initiating door-closing sequence", elevator.Id);
            elevator.Status = ElevatorStatus.ClosingDoors;
            elevator.DoorStatus = DoorStatus.Closing;
            _doorTimers[elevator.Id] = 0;

            _logger.LogInformation("22.💾 Saving door-closing state to database for elevator {ElevatorId}", elevator.Id);
            await context.SaveChangesAsync();
            _logger.LogInformation("22.✅ Door-closing state saved for elevator {ElevatorId}", elevator.Id);
        }
    }

    private async Task MarkCallAsHandledAsync(ApplicationDbContext context, ElevatorCall call)
    {
        call.IsHandled = true;
        _logger.LogInformation("23.💾 Saving handled call to database (CallId: {CallId})", call.Id);
        await context.SaveChangesAsync();
        _logger.LogInformation("23.✅ Call {CallId} marked as handled and saved", call.Id);
    }

    public async Task SendElevatorUpdateAsync(int elevatorId, ElevatorUpdateMessage message)
    {
        if (message == null)
        {
            _logger.LogError("ElevatorUpdateMessage is null for elevator {ElevatorId}", elevatorId);
            return;
        }
        // הדפס את כל השדות של message ללוג
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(message);
            _logger.LogInformation("ElevatorUpdateMessage for elevator {ElevatorId}: {MessageJson}", elevatorId, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize ElevatorUpdateMessage for elevator {ElevatorId}", elevatorId);
        }
        _logger.LogInformation("24.📡 Sending SignalR update to group 'elevator_{ElevatorId}'", elevatorId);
        await _hubContext.Clients.Group($"elevator_{elevatorId}")
            .SendAsync("ReceiveElevatorUpdate", message);
        _logger.LogInformation("24.✅ SignalR update sent to group 'elevator_{ElevatorId}'", elevatorId);
    }
}
