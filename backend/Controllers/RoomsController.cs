using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelly.Backend.Auth;
using Shelly.Backend.Data;
using Shelly.Backend.Entities;

namespace Shelly.Backend.Controllers;

[ApiController]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly ShellyDbContext _db;

    public RoomsController(ShellyDbContext db) { _db = db; }

    [HttpPost("workspaces/{workspaceId:guid}/rooms")]
    public async Task<ActionResult<RoomDto>> Create(Guid workspaceId, [FromBody] CreateRoomRequest req)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "name required" });

        var member = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (member is null) return NotFound();

        var name = req.Name.Trim();
        var clash = await _db.Rooms
            .AnyAsync(r => r.WorkspaceId == workspaceId && r.Name == name);
        if (clash) return Conflict(new { error = "room name already exists in this workspace" });

        var room = new Room
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            CreatedBy = userId.Value,
        };
        _db.Rooms.Add(room);
        _db.RoomMembers.Add(new RoomMember
        {
            RoomId = room.Id,
            UserId = userId.Value,
            Role = RoomRoles.Owner,
        });
        await _db.SaveChangesAsync();

        return Ok(new RoomDto(
            room.Id, room.WorkspaceId, room.Name, room.Description,
            RoomRoles.Owner, room.CreatedAt));
    }

    [HttpGet("workspaces/{workspaceId:guid}/rooms")]
    public async Task<ActionResult<List<RoomDto>>> List(Guid workspaceId)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var isMember = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        if (!isMember) return NotFound();

        var rooms = await _db.Rooms
            .Where(r => r.WorkspaceId == workspaceId && r.ArchivedAt == null)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new { r.Id, r.WorkspaceId, r.Name, r.Description, r.CreatedAt })
            .ToListAsync();

        var ids = rooms.Select(r => r.Id).ToList();
        var roles = await _db.RoomMembers
            .Where(rm => ids.Contains(rm.RoomId) && rm.UserId == userId)
            .ToDictionaryAsync(rm => rm.RoomId, rm => rm.Role);

        var dtos = rooms.Select(r => new RoomDto(
            r.Id, r.WorkspaceId, r.Name, r.Description,
            roles.TryGetValue(r.Id, out var role) ? role : RoomRoles.Viewer,
            r.CreatedAt)).ToList();

        return Ok(dtos);
    }

    [HttpGet("rooms/{id:guid}")]
    public async Task<ActionResult<RoomDto>> Get(Guid id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id);
        if (room is null) return NotFound();

        var wsMember = await _db.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == room.WorkspaceId && m.UserId == userId);
        if (!wsMember) return NotFound();

        var roomRole = await _db.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == id && rm.UserId == userId);

        return Ok(new RoomDto(
            room.Id, room.WorkspaceId, room.Name, room.Description,
            roomRole?.Role ?? RoomRoles.Viewer, room.CreatedAt));
    }

    [HttpPatch("rooms/{id:guid}")]
    public async Task<ActionResult<RoomDto>> Update(Guid id, [FromBody] UpdateRoomRequest req)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id);
        if (room is null) return NotFound();

        var wsMember = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == room.WorkspaceId && m.UserId == userId);
        if (wsMember is null) return NotFound();

        var roomRole = await _db.RoomMembers
            .FirstOrDefaultAsync(rm => rm.RoomId == id && rm.UserId == userId);
        var effectiveRole = roomRole?.Role ?? RoomRoles.Viewer;
        var canEdit = effectiveRole == RoomRoles.Owner
            || wsMember.Role == WorkspaceRoles.Owner
            || wsMember.Role == WorkspaceRoles.Admin;
        if (!canEdit) return Forbid();

        if (!string.IsNullOrWhiteSpace(req.Name))
        {
            var newName = req.Name.Trim();
            var clash = await _db.Rooms.AnyAsync(r =>
                r.WorkspaceId == room.WorkspaceId && r.Name == newName && r.Id != room.Id);
            if (clash) return Conflict(new { error = "room name already exists" });
            room.Name = newName;
        }
        if (req.Description is not null)
            room.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();

        await _db.SaveChangesAsync();
        return Ok(new RoomDto(
            room.Id, room.WorkspaceId, room.Name, room.Description,
            effectiveRole, room.CreatedAt));
    }

    [HttpDelete("rooms/{id:guid}")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id);
        if (room is null) return NotFound();

        var wsMember = await _db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == room.WorkspaceId && m.UserId == userId);
        if (wsMember is null) return NotFound();

        if (wsMember.Role != WorkspaceRoles.Owner && wsMember.Role != WorkspaceRoles.Admin)
            return Forbid();

        room.ArchivedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}