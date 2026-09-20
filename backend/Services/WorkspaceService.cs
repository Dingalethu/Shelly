using Microsoft.EntityFrameworkCore;
using Shelly.Backend.Data;
using Shelly.Backend.Entities;

namespace Shelly.Backend.Services;

public class WorkspaceService
{
    private readonly ShellyDbContext _db;

    public WorkspaceService(ShellyDbContext db) { _db = db; }

    public async Task<Workspace> CreateAsync(string name, Guid creatorId)
    {
        var baseSlug = Slugger.Slugify(name);
        var slug = baseSlug;
        var suffix = 2;
        while (await _db.Workspaces.AnyAsync(w => w.Slug == slug))
            slug = $"{baseSlug}-{suffix++}";

        var ws = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug,
            CreatedBy = creatorId,
        };
        _db.Workspaces.Add(ws);
        _db.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = ws.Id,
            UserId = creatorId,
            Role = WorkspaceRoles.Owner,
        });
        await _db.SaveChangesAsync();
        return ws;
    }
}