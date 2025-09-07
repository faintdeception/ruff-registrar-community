using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRegistrar.Api.DTOs;
using StudentRegistrar.Api.Services;
using StudentRegistrar.Models;

namespace StudentRegistrar.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(
        IRoomService roomService,
        ILogger<RoomsController> logger)
    {
        _roomService = roomService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous] // Allow anonymous access to view rooms
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRooms()
    {
        try
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // Allow anonymous access to view a room
    public async Task<ActionResult<RoomDto>> GetRoom(Guid id)
    {
        try
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null)
                return NotFound();

            return Ok(room);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving room {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("by-type/{roomType}")]
    [AllowAnonymous] // Allow anonymous access to view rooms by type
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetRoomsByType(RoomType roomType)
    {
        try
        {
            var rooms = await _roomService.GetRoomsByTypeAsync(roomType);
            return Ok(rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving rooms by type {RoomType}", roomType);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<RoomDto>> CreateRoom(CreateRoomDto createDto)
    {
        try
        {
            var room = await _roomService.CreateRoomAsync(createDto);
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating room");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult<RoomDto>> UpdateRoom(Guid id, UpdateRoomDto updateDto)
    {
        try
        {
            var room = await _roomService.UpdateRoomAsync(id, updateDto);
            if (room == null)
                return NotFound();

            return Ok(room);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating room {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating room {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Administrator")]
    public async Task<ActionResult> DeleteRoom(Guid id)
    {
        try
        {
            var deleted = await _roomService.DeleteRoomAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete room {Id}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting room {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}/in-use")]
    public async Task<ActionResult<bool>> IsRoomInUse(Guid id)
    {
        try
        {
            var inUse = await _roomService.IsRoomInUseAsync(id);
            return Ok(inUse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if room {Id} is in use", id);
            return StatusCode(500, "Internal server error");
        }
    }
}
