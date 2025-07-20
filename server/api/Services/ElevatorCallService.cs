// Elevator call service implementation - manages elevator call creation, updates, and queries
// Handles call validation, duplicate prevention, and building-specific call management

using ElevatorSimulationApi.Data;
using ElevatorSimulationApi.Models.DTOs;
using ElevatorSimulationApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElevatorSimulationApi.Services;

public class ElevatorCallService : IElevatorCallService
{
    private readonly ApplicationDbContext _context;

    public ElevatorCallService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Create new elevator call with floor validation and duplicate prevention
    public async Task<ElevatorCallResponse> CreateCallAsync(CreateElevatorCallRequest request)
    {
        Console.WriteLine($"Creating elevator call for building {request.BuildingId}, floor {request.RequestedFloor}");
        var building = await _context.Buildings.FirstOrDefaultAsync(b => b.Id == request.BuildingId);
        if (building == null)
        {
            Console.WriteLine($"Building {request.BuildingId} not found in database");
            throw new InvalidOperationException("Building not found");
        }
        Console.WriteLine($"Found building: {building.Name} with {building.NumberOfFloors} floors");

        if (request.RequestedFloor < 0 || request.RequestedFloor >= building.NumberOfFloors)
        {
            throw new InvalidOperationException("Invalid floor number");
        }

        if (request.DestinationFloor.HasValue && (request.DestinationFloor.Value < 0 || request.DestinationFloor.Value >= building.NumberOfFloors))
        {
            throw new InvalidOperationException("Invalid destination floor number");
        }

        // Prevent duplicate active calls for the same floor in the same building
        var existingCall = await _context.ElevatorCalls.FirstOrDefaultAsync(c =>
            c.BuildingId == request.BuildingId &&
            c.RequestedFloor == request.RequestedFloor &&
            !c.IsHandled &&
            c.DestinationFloor == null // Only for floor calls, not elevator internal requests
        );
        if (existingCall != null)
        {
            Console.WriteLine($"Duplicate call detected for building {request.BuildingId}, floor {request.RequestedFloor}. Returning existing call.");
            return new ElevatorCallResponse
            {
                Id = existingCall.Id,
                BuildingId = existingCall.BuildingId,
                RequestedFloor = existingCall.RequestedFloor,
                DestinationFloor = existingCall.DestinationFloor,
                CallTime = existingCall.CallTime,
                IsHandled = existingCall.IsHandled
            };
        }

        var call = new ElevatorCall
        {
            BuildingId = request.BuildingId,
            RequestedFloor = request.RequestedFloor,
            DestinationFloor = request.DestinationFloor,
            CallTime = DateTime.UtcNow,
            IsHandled = false
        };

        _context.ElevatorCalls.Add(call);
        await _context.SaveChangesAsync();

        return new ElevatorCallResponse
        {
            Id = call.Id,
            BuildingId = call.BuildingId,
            RequestedFloor = call.RequestedFloor,
            DestinationFloor = call.DestinationFloor,
            CallTime = call.CallTime,
            IsHandled = call.IsHandled
        };
    }

    // Update existing elevator call with destination floor
    public async Task<ElevatorCallResponse> UpdateCallAsync(int callId, UpdateElevatorCallRequest request)
    {
        var call = await _context.ElevatorCalls
            .Include(c => c.Building)
            .FirstOrDefaultAsync(c => c.Id == callId);

        if (call == null)
        {
            throw new InvalidOperationException("Call not found");
        }

        if (request.DestinationFloor < 0 || request.DestinationFloor >= call.Building.NumberOfFloors)
        {
            throw new InvalidOperationException("Invalid destination floor number");
        }

        call.DestinationFloor = request.DestinationFloor;
        await _context.SaveChangesAsync();

        return new ElevatorCallResponse
        {
            Id = call.Id,
            BuildingId = call.BuildingId,
            RequestedFloor = call.RequestedFloor,
            DestinationFloor = call.DestinationFloor,
            CallTime = call.CallTime,
            IsHandled = call.IsHandled
        };
    }

    // Get all elevator calls for a specific building ordered by time
    public async Task<List<ElevatorCallResponse>> GetBuildingCallsAsync(int buildingId)
    {
        var calls = await _context.ElevatorCalls
            .Where(c => c.BuildingId == buildingId)
            .OrderByDescending(c => c.CallTime)
            .Select(c => new ElevatorCallResponse
            {
                Id = c.Id,
                BuildingId = c.BuildingId,
                RequestedFloor = c.RequestedFloor,
                DestinationFloor = c.DestinationFloor,
                CallTime = c.CallTime,
                IsHandled = c.IsHandled
            })
            .ToListAsync();

        return calls;
    }
} 