using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shelly.Backend.Auth;
using Shelly.Backend.Data;
using Shelly.Backend.Entities;
using Shelly.Backend.Services;

namespace Shelly.Backend.Controllers;

[ApiController]
[Authorize]
[Route("workspaces")]
public class WorkspacesController : ControllerBase
{
    private readonly ShellyDbContext _db;
    private readonly WorkspaceService _workspaces;

    public WorkspacesController(ShellyDbContext db, WorkspaceService workspaces)
    {
        _db = db;
        _workspaces = workspaces;
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceDto>> Create([FromBody] CreateWorkspaceRequest req)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "name required" });

        var ws = await _workspaces.CreateAsync(req.Name, userId.Value);
        return Ok(new WorkspaceDto(ws.Id, ws.Name, ws.Slug, WorkspaceRoles.Owner, ws.CreatedAt));
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkspaceDto>>> List()
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var list = await _db.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.JoinedAt)
            .Select(m => new WorkspaceDto(
                m.Workspace!.Id,
                m.Workspace.Name,
                m.Workspace.Slug,
                m.Role,
                m.Workspace.CreatedAt))
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> Get(Guid id)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var m = await _db.WorkspaceMembers
            .Include(x => x.Workspace)
            .FirstOrDefaultAsync(x => x.WorkspaceId == id && x.UserId == userId);
        if (m is null) return NotFound();

        return Ok(new WorkspaceDto(
            m.Workspace!.Id, m.Workspace.Name, m.Workspace.Slug, m.Role, m.Workspace.CreatedAt));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<WorkspaceDto>> Update(Guid id, [FromBody] UpdateWorkspaceRequest req)
    {
        var userId = User.GetUserId();
        if (userId is null) return Unauthorized();

        var m = await _db.WorkspaceMembers
            .Include(x => x.Workspace)
            .FirstOrDefaultAsync(x => x.WorkspaceId == id && x.UserId == userId);
        if (m is null) return NotFound();

        if (m.Role != WorkspaceRoles.Owner && m.Role != WorkspaceRoles.Admin)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(req.Name))
            m.Workspace!.Name = req.Name.Trim();

        await _db.SaveChangesAsync();
        return Ok(new WorkspaceDto(
            m.Workspace!.Id, m.Workspace.Name, m.Workspace.Slug, m.Role, m.Workspace.CreatedAt));
    }
}