using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shelly.Backend.Auth;
using Shelly.Backend.Entities;

namespace Shelly.Backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _users;
    private readonly JwtTokenService _tokens;

    public AuthController(UserManager<User> users, JwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password) ||
            string.IsNullOrWhiteSpace(req.DisplayName))
            return BadRequest(new { error = "email, password, displayName are required" });

        var existing = await _users.FindByEmailAsync(req.Email);
        if (existing is not null)
            return Conflict(new { error = "email already registered" });

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName.Trim(),
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { error = "registration failed", details = result.Errors.Select(e => e.Description) });

        var (token, expiresAt) = _tokens.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAt,
            new UserDto(user.Id, user.Email!, user.DisplayName, user.AvatarUrl)));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user is null || !await _users.CheckPasswordAsync(user, req.Password))
            return Unauthorized(new { error = "invalid credentials" });

        var (token, expiresAt) = _tokens.CreateToken(user);
        return Ok(new AuthResponse(token, expiresAt,
            new UserDto(user.Id, user.Email!, user.DisplayName, user.AvatarUrl)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var idClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (idClaim is null || !Guid.TryParse(idClaim, out var id))
            return Unauthorized();

        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return Unauthorized();

        return Ok(new UserDto(user.Id, user.Email!, user.DisplayName, user.AvatarUrl));
    }
}