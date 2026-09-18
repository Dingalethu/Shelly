namespace Shelly.Backend.Entities;

public static class WorkspaceRoles
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Member = "member";
}

public class WorkspaceMember
{
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = WorkspaceRoles.Member;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public Workspace? Workspace { get; set; }
    public User? User { get; set; }
}