using Microsoft.AspNetCore.Identity;

namespace Shelly.Backend.Entities;

public class User : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}