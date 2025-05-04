using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Controllers;

using Microsoft.AspNetCore.Mvc;
using Tutorial8.Services;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly ITripsService _tripsService;

    public ClientsController(ITripsService tripsService)
    {
        _tripsService = tripsService;
    }

    [HttpGet("{id}/trips")]
    public async Task<IActionResult> GetClientTrips(int id)
    {
        var trips = await _tripsService.GetTripsForClient(id);
        if (!trips.Any())
        {
            return NotFound($"Client with ID {id} has no trips or does not exist.");
        }

        return Ok(trips);
    }
    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDTO clientDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
    
        try
        {
            var id = await _tripsService.CreateClient(clientDto);
            return CreatedAtAction(nameof(GetClientTrips), new { id }, new { IdClient = id });
        }
        catch (SqlException ex) when (ex.Number == 2627)
        {
            return Conflict("Client with this PESEL already exists");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("{id}/trips/{tripId}")]
    public async Task<IActionResult> AssignClientToTrip(int id, int tripId)
    {
        var assignDto = new ClientTripAssignDTO { IdClient = id, IdTrip = tripId };
    
        try
        {
            var success = await _tripsService.AssignClientToTrip(assignDto);
            if (!success)
                return BadRequest("Assignment failed - client or trip not found, or already assigned");
            
            return Ok("Client successfully assigned to trip");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete("{id}/trips/{tripId}")]
    public async Task<IActionResult> RemoveClientFromTrip(int id, int tripId)
    {
        try
        {
            var success = await _tripsService.RemoveClientFromTrip(id, tripId);
            if (!success)
                return NotFound("Assignment not found");
            
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}