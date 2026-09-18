namespace Shelly.Backend.Entities;

public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkspaceId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Workspace? Workspace { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
}