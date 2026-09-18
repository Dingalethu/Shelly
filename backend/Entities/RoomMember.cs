namespace Shelly.Backend.Entities;

public static class RoomRoles
{
    public const string Owner = "owner";
    public const string Operator = "operator";
    public const string Viewer = "viewer";
}

public class RoomMember
{
    public Guid RoomId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = RoomRoles.Viewer;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public Room? Room { get; set; }
    public User? User { get; set; }
}