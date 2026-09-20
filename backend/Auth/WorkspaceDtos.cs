namespace Shelly.Backend.Auth;

public record WorkspaceDto(
    Guid Id,
    string Name,
    string Slug,
    string Role,
    DateTimeOffset CreatedAt);

public record CreateWorkspaceRequest(string Name);
public record UpdateWorkspaceRequest(string? Name);

public record RoomDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string? Description,
    string Role,
    DateTimeOffset CreatedAt);

public record CreateRoomRequest(string Name, string? Description);
public record UpdateRoomRequest(string? Name, string? Description);