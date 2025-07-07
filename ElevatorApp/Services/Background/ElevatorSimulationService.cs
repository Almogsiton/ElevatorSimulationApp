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

namespace ElevatorApp.Services.Background
{
    /// <summary>
    /// Background service responsible for simulating elevator movement and state updates.
    /// </summary>
    public class ElevatorSimulationService : BackgroundService
    {
        private readonly ILogger<ElevatorSimulationService> _logger;
        private readonly ElevatorDbContext _context;
        private readonly Dictionary<int, int> _doorTimers = new();
        private readonly IHubContext<ElevatorHub> _hubContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElevatorSimulationService"/> class.
        /// </summary>
        /// <param name="logger">The logger used to track service execution.</param>
        /// <param name="context">The database context for accessing elevator and call data.</param>
        public ElevatorSimulationService(ILogger<ElevatorSimulationService> logger, ElevatorDbContext context, IHubContext<ElevatorHub> hubContext)
        {
            _logger = logger;
            _context = context;
            _hubContext = hubContext;
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

                    var elevators = await LoadElevatorsAsync(stoppingToken);
                    var pendingCalls = await LoadPendingCallsAsync(stoppingToken);

                    AssignElevatorsToCalls(elevators, pendingCalls);
                    MoveElevators(elevators);
                    await HandleElevatorArrivals(elevators, stoppingToken);
                    HandleDoorTimers(elevators);
                    await BroadcastElevatorUpdates(elevators, stoppingToken);


                    await _context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during simulation heartbeat.");
                }

                await Task.Delay(3000, stoppingToken);
            }

            _logger.LogInformation("Elevator simulation service stopping.");
        }

        /// <summary>
        /// Loads all elevators from the database.
        /// </summary>
        private Task<List<Elevator>> LoadElevatorsAsync(CancellationToken ct) =>
            _context.Elevators.ToListAsync(ct);

        /// <summary>
        /// Loads all elevator calls that have not been handled yet.
        /// </summary>
        private Task<List<ElevatorCall>> LoadPendingCallsAsync(CancellationToken ct) =>
            _context.ElevatorCalls
                .Where(call => !call.IsHandled)
                .OrderBy(call => call.CallTime)
                .ToListAsync(ct);

        /// <summary>
        /// Assigns the first available elevator to the earliest unhandled call.
        /// </summary>
        private void AssignElevatorsToCalls(List<Elevator> elevators, List<ElevatorCall> pendingCalls)
        {
            foreach (var call in pendingCalls)
            {
                var idleElevator = elevators.FirstOrDefault(e => e.Status == ElevatorStatus.Idle);
                if (idleElevator != null)
                {
                    var direction = call.RequestedFloor > idleElevator.CurrentFloor
                        ? ElevatorDirection.Up
                        : ElevatorDirection.Down;

                    idleElevator.Status = direction == ElevatorDirection.Up
                        ? ElevatorStatus.MovingUp
                        : ElevatorStatus.MovingDown;

                    idleElevator.Direction = direction;
                    call.IsHandled = true;

                    _context.ElevatorCallAssignments.Add(new ElevatorCallAssignment
                    {
                        ElevatorId = idleElevator.Id,
                        ElevatorCallId = call.Id,
                        AssignmentTime = DateTime.Now
                    });

                    _logger.LogInformation($"Assigned elevator {idleElevator.Id} to call {call.Id}");
                    break;
                }
            }
        }

        /// <summary>
        /// Moves all elevators one floor in the direction they are currently moving.
        /// </summary>
        private void MoveElevators(List<Elevator> elevators)
        {
            foreach (var elevator in elevators)
            {
                if (elevator.Status == ElevatorStatus.MovingUp && elevator.Direction == ElevatorDirection.Up)
                {
                    elevator.CurrentFloor++;
                    _logger.LogInformation($"Elevator {elevator.Id} moved up to floor {elevator.CurrentFloor}");
                }
                else if (elevator.Status == ElevatorStatus.MovingDown && elevator.Direction == ElevatorDirection.Down)
                {
                    elevator.CurrentFloor--;
                    _logger.LogInformation($"Elevator {elevator.Id} moved down to floor {elevator.CurrentFloor}");
                }
            }
        }
        /// <summary>
        /// Checks if any elevator has reached its target floor and opens doors if so.
        /// </summary>
        private async Task HandleElevatorArrivals(List<Elevator> elevators, CancellationToken ct)
        {
            var assignments = await _context.ElevatorCallAssignments
                .Include(a => a.ElevatorCall)
                .ToListAsync(ct);

            foreach (var elevator in elevators)
            {
                var assignment = assignments.FirstOrDefault(a => a.ElevatorId == elevator.Id);

                if (assignment == null)
                    continue;

                if (assignment?.ElevatorCall == null)
                    continue;

                var targetFloor = assignment.ElevatorCall.RequestedFloor;


                if (elevator.CurrentFloor == targetFloor &&
                    (elevator.Status == ElevatorStatus.MovingUp || elevator.Status == ElevatorStatus.MovingDown))
                {
                    elevator.Status = ElevatorStatus.OpeningDoors;
                    elevator.DoorStatus = DoorStatus.Open;

                    _logger.LogInformation($"Elevator {elevator.Id} arrived at floor {elevator.CurrentFloor} and opened doors.");
                }
            }
        }

        /// <summary>
        /// Handles door timers and closes elevator doors after a defined number of ticks.
        /// </summary>
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
                        elevator.Status = ElevatorStatus.Idle;
                        elevator.Direction = ElevatorDirection.None;

                        _doorTimers.Remove(elevator.Id);
                        _logger.LogInformation($"Elevator {elevator.Id} closed doors and is now idle.");
                    }
                }
                else
                {
                    // Reset timer if elevator moved away from open-door state
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
