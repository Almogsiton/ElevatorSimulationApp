using ElevatorSimulationApi.Models.DTOs;
using ElevatorSimulationApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElevatorSimulationApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ElevatorCallsController : ControllerBase
{
    private readonly IElevatorCallService _elevatorCallService;

    public ElevatorCallsController(IElevatorCallService elevatorCallService)
    {
        _elevatorCallService = elevatorCallService;
    }

    [HttpPost("create")]
    public async Task<ActionResult<ElevatorCallResponse>> CreateCall(CreateElevatorCallRequest request)
    {
        try
        {
            var call = await _elevatorCallService.CreateCallAsync(request);
            return CreatedAtAction(nameof(GetBuildingCalls), new { buildingId = call.BuildingId }, call);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("update/{id}")]
    public async Task<ActionResult<ElevatorCallResponse>> UpdateCall(int id, UpdateElevatorCallRequest request)
    {
        try
        {
            var call = await _elevatorCallService.UpdateCallAsync(id, request);
            return Ok(call);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("get/building/calls/{buildingId}")]
    public async Task<ActionResult<List<ElevatorCallResponse>>> GetBuildingCalls(int buildingId)
    {
        try
        {
            var calls = await _elevatorCallService.GetBuildingCallsAsync(buildingId);
            return Ok(calls);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
} 