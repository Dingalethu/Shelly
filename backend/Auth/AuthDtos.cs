namespace Shelly.Backend.Auth;

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? AvatarUrl);