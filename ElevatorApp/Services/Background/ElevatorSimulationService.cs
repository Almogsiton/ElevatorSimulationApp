using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ElevatorApp.DataAccess.Context;
using ElevatorApp.DataAccess.Helpers;
using ElevatorApp.DataAccess.Entities;
using ElevatorApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace ElevatorApp.Services.Background
{
    /// <summary>
    /// Background service responsible for simulating elevator movement and state updates.
    /// </summary>
    public class ElevatorSimulationService : BackgroundService
    {
        private readonly ILogger<ElevatorSimulationService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Dictionary<int, int> _doorTimers = new();
        private readonly IHubContext<ElevatorHub> _hubContext;
        private readonly int _tickInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElevatorSimulationService"/> class.
        /// </summary>
        /// <param name="logger">The logger used to track service execution.</param>
        /// <param name="context">The database context for accessing elevator and call data.</param>
        public ElevatorSimulationService(
    ILogger<ElevatorSimulationService> logger,
    IServiceScopeFactory scopeFactory,
    IHubContext<ElevatorHub> hubContext,
    IOptions<SimulationSettings> settings)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _tickInterval = settings.Value.TickIntervalMilliseconds;
        }


        /// <summary>
        /// Executes the background simulation loop for elevator updates.
        /// </summary>
        /// <param name="stoppingToken">Token to signal cancellation.</param>
        /// <returns>A Task that represents the background process.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Elevator simulation service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Simulation heartbeat at: {time}", DateTimeOffset.Now);

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ElevatorDbContext>();

                        var elevators = await context.Elevators.ToListAsync(stoppingToken);
                        var pendingCalls = await context.ElevatorCalls
                            .Where(call => !call.IsHandled)
                            .OrderBy(call => call.CallTime)
                            .ToListAsync(stoppingToken);

                        AssignElevatorsToCalls(context, elevators, pendingCalls);
                        MoveElevators(elevators);
                        await HandleElevatorArrivals(context, elevators, stoppingToken);
                        HandleDoorTimers(elevators);
                        await BroadcastElevatorUpdates(elevators, stoppingToken);

                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during simulation heartbeat.");
                }

                await Task.Delay(_tickInterval, stoppingToken);
            }

            _logger.LogInformation("Elevator simulation service stopping.");
        }

        /// <summary>
        /// Assigns unhandled calls to elevators that are idle or already moving in a matching direction.
        /// </summary>
        private void AssignElevatorsToCalls(ElevatorDbContext context, List<Elevator> elevators, List<ElevatorCall> pendingCalls)
        {
            foreach (var call in pendingCalls)
            {
                if (TryAssignToMovingElevator(context, elevators, call))
                    continue;

                TryAssignToIdleElevator(context, elevators, call);
            }
        }

        /// <summary>
        /// Attempts to assign the call to an elevator already moving in the right direction and path.
        /// </summary>
        private bool TryAssignToMovingElevator(ElevatorDbContext context, List<Elevator> elevators, ElevatorCall call)
        {
            foreach (var elevator in elevators)
            {
                bool isMovingUp = elevator.Status == ElevatorStatus.MovingUp && call.RequestedFloor > elevator.CurrentFloor;
                bool isMovingDown = elevator.Status == ElevatorStatus.MovingDown && call.RequestedFloor < elevator.CurrentFloor;

                if ((isMovingUp || isMovingDown) && !elevator.TargetFloors.Contains(call.RequestedFloor))
                {
                    elevator.TargetFloors.Add(call.RequestedFloor);
                    elevator.TargetFloors = isMovingUp
                        ? elevator.TargetFloors.Distinct().OrderBy(f => f).ToList()
                        : elevator.TargetFloors.Distinct().OrderByDescending(f => f).ToList();

                    call.IsHandled = true;

                    context.ElevatorCallAssignments.Add(new ElevatorCallAssignment
                    {
                        ElevatorId = elevator.Id,
                        ElevatorCallId = call.Id,
                        AssignmentTime = DateTime.Now
                    });

                    _logger.LogInformation($"(Moving) Added call {call.Id} to elevator {elevator.Id} at floor {call.RequestedFloor}");
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Assigns the call to an idle elevator and initializes movement.
        /// </summary>
        private void TryAssignToIdleElevator(ElevatorDbContext context, List<Elevator> elevators, ElevatorCall call)
        {
            var idleElevator = elevators.FirstOrDefault(e => e.Status == ElevatorStatus.Idle);
            if (idleElevator == null) return;

            var direction = call.RequestedFloor > idleElevator.CurrentFloor
                ? ElevatorDirection.Up
                : ElevatorDirection.Down;

            idleElevator.Status = direction == ElevatorDirection.Up
                ? ElevatorStatus.MovingUp
                : ElevatorStatus.MovingDown;

            idleElevator.Direction = direction;
            idleElevator.TargetFloors.Add(call.RequestedFloor);

            idleElevator.TargetFloors = direction == ElevatorDirection.Up
                ? idleElevator.TargetFloors.Distinct().OrderBy(f => f).ToList()
                : idleElevator.TargetFloors.Distinct().OrderByDescending(f => f).ToList();

            call.IsHandled = true;

            context.ElevatorCallAssignments.Add(new ElevatorCallAssignment
            {
                ElevatorId = idleElevator.Id,
                ElevatorCallId = call.Id,
                AssignmentTime = DateTime.Now
            });

            _logger.LogInformation($"(Idle) Assigned elevator {idleElevator.Id} to call {call.Id} at floor {call.RequestedFloor}");
        }

        /// <summary>
        /// Moves all elevators one floor toward their next target floor, if any.
        /// </summary>
        private void MoveElevators(List<Elevator> elevators)
        {
            foreach (var elevator in elevators)
            {
                if (elevator.TargetFloors == null || !elevator.TargetFloors.Any())
                    continue;

                int nextTarget = elevator.TargetFloors.First();

                if (elevator.CurrentFloor < nextTarget)
                {
                    elevator.CurrentFloor++;
                    elevator.Direction = ElevatorDirection.Up;
                    elevator.Status = ElevatorStatus.MovingUp;
                    _logger.LogInformation($"Elevator {elevator.Id} moved up to floor {elevator.CurrentFloor}");
                }
                else if (elevator.CurrentFloor > nextTarget)
                {
                    elevator.CurrentFloor--;
                    elevator.Direction = ElevatorDirection.Down;
                    elevator.Status = ElevatorStatus.MovingDown;
                    _logger.LogInformation($"Elevator {elevator.Id} moved down to floor {elevator.CurrentFloor}");
                }
            }
        }

        /// <summary>
        /// Handles elevator arrivals at their next target floor.
        /// Opens doors and removes the floor from the target list.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="elevators">The list of elevators.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task HandleElevatorArrivals(ElevatorDbContext context, List<Elevator> elevators, CancellationToken ct)
        {
            var assignments = await context.ElevatorCallAssignments
                .Include(a => a.ElevatorCall)
                .ToListAsync(ct);

            foreach (var elevator in elevators)
            {
                if (elevator.TargetFloors == null || !elevator.TargetFloors.Any())
                    continue;

                int targetFloor = elevator.TargetFloors.First();

                if (elevator.CurrentFloor == targetFloor &&
                    (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown))
                {
                    elevator.Status = ElevatorStatus.OpeningDoors;
                    elevator.DoorStatus = DoorStatus.Open;

                    elevator.TargetFloors.RemoveAt(0);

                    _logger.LogInformation($"Elevator {elevator.Id} arrived at floor {targetFloor} and opened doors.");
                }
            }
        }

        /// <summary>
        /// Closes elevator doors after a predefined number of ticks and resumes movement if there are more targets.
        /// </summary>
        /// <param name="elevators">The list of elevators.</param>
        private void HandleDoorTimers(List<Elevator> elevators)
        {
            foreach (var elevator in elevators)
            {
                if (elevator.Status == ElevatorStatus.OpeningDoors && elevator.DoorStatus == DoorStatus.Open)
                {
                    if (!_doorTimers.ContainsKey(elevator.Id))
                    {
                        _doorTimers[elevator.Id] = 1;
                    }
                    else
                    {
                        _doorTimers[elevator.Id]++;
                    }

                    if (_doorTimers[elevator.Id] >= Constants.DoorOpenTicks)
                    {
                        elevator.DoorStatus = DoorStatus.Closed;

                        if (elevator.TargetFloors.Any())
                        {
                            int nextTarget = elevator.TargetFloors.First();

                            if (nextTarget > elevator.CurrentFloor)
                            {
                                elevator.Status = ElevatorStatus.MovingUp;
                                elevator.Direction = ElevatorDirection.Up;
                            }
                            else if (nextTarget < elevator.CurrentFloor)
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
                        _logger.LogInformation($"Elevator {elevator.Id} closed doors. New status: {elevator.Status}");
                    }
                }
                else
                {
                    if (_doorTimers.ContainsKey(elevator.Id))
                    {
                        _doorTimers.Remove(elevator.Id);
                    }
                }
            }
        }


        /// <summary>
        /// Sends elevator state updates to all connected clients via SignalR.
        /// </summary>
        private async Task BroadcastElevatorUpdates(List<Elevator> elevators, CancellationToken ct)
        {
            foreach (var elevator in elevators)
            {
                var update = new
                {
                    elevatorId = elevator.Id,
                    currentFloor = elevator.CurrentFloor,
                    status = elevator.Status.ToString(),
                    direction = elevator.Direction.ToString(),
                    doorStatus = elevator.DoorStatus.ToString()
                };

                await _hubContext.Clients.All.SendAsync("ReceiveElevatorUpdate", update, ct);
            }
        }


    }

}
