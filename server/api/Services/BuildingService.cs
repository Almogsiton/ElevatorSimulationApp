using ElevatorSimulationApi.Data;
using ElevatorSimulationApi.Models.DTOs;
using ElevatorSimulationApi.Models.Entities;
using ElevatorSimulationApi.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ElevatorSimulationApi.Services;

public class BuildingService : IBuildingService
{
    private readonly ApplicationDbContext _context;

    public BuildingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BuildingListResponse>> GetUserBuildingsAsync(int userId)
    {
        var buildings = await _context.Buildings
            .Where(b => b.UserId == userId)
            .Select(b => new BuildingListResponse
            {
                Id = b.Id,
                Name = b.Name,
                NumberOfFloors = b.NumberOfFloors
            })
            .ToListAsync();

        return buildings;
    }

    public async Task<BuildingResponse> GetBuildingAsync(int buildingId, int userId)
    {
        var building = await _context.Buildings
            .Include(b => b.Elevator)
            .FirstOrDefaultAsync(b => b.Id == buildingId && b.UserId == userId);

        if (building == null)
        {
            throw new InvalidOperationException("Building not found");
        }

        return new BuildingResponse
        {
            Id = building.Id,
            Name = building.Name,
            NumberOfFloors = building.NumberOfFloors,
            UserId = building.UserId,
            Elevator = new ElevatorResponse
            {
                Id = building.Elevator.Id,
                BuildingId = building.Elevator.BuildingId,
                CurrentFloor = building.Elevator.CurrentFloor,
                Status = building.Elevator.Status,
                Direction = building.Elevator.Direction,
                DoorStatus = building.Elevator.DoorStatus
            }
        };
    }

    public async Task<BuildingResponse> CreateBuildingAsync(CreateBuildingRequest request, int userId)
    {
        var building = new Building
        {
            Name = request.Name,
            NumberOfFloors = request.NumberOfFloors,
            UserId = userId
        };

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync();

        var elevator = new Elevator
        {
            BuildingId = building.Id,
            CurrentFloor = 0,
            Status = ElevatorStatus.Idle,
            Direction = ElevatorDirection.None,
            DoorStatus = DoorStatus.Closed
        };

        _context.Elevators.Add(elevator);
        await _context.SaveChangesAsync();

        return new BuildingResponse
        {
            Id = building.Id,
            Name = building.Name,
            NumberOfFloors = building.NumberOfFloors,
            UserId = building.UserId,
            Elevator = new ElevatorResponse
            {
                Id = elevator.Id,
                BuildingId = elevator.BuildingId,
                CurrentFloor = elevator.CurrentFloor,
                Status = elevator.Status,
                Direction = elevator.Direction,
                DoorStatus = elevator.DoorStatus
            }
        };
    }

    public async Task<List<Building>> GetAllBuildingsWithDapperAsync()
    {
        using (var connection = new SqlConnection(_context.Database.GetDbConnection().ConnectionString))
        {
            string sql = "SELECT * FROM Buildings";
            var buildings = await connection.QueryAsync<Building>(sql);
            return buildings.ToList();
        }
    }
} 