using System.Collections.Concurrent;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shelly.Backend.Data;
using Shelly.Backend.Entities;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shelly.Backend.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShellyDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentityCore<User>(opt =>
    {
        opt.Password.RequiredLength = 8;
        opt.Password.RequireDigit = true;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ShellyDbContext>();

// JWT settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

// Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? "";
var jwtIssuer = jwtSection["Issuer"] ?? "";
var jwtAudience = jwtSection["Audience"] ?? "";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.UseWebSockets();

var rooms = new ConcurrentDictionary<string, TerminalRoom>();

app.MapControllers();

app.Map("/ws/agent/{room}", async (HttpContext ctx, string room) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var r = rooms.GetOrAdd(room, _ => new TerminalRoom(room));
    await r.AttachAgentAsync(ws);
});

app.Map("/ws/terminal/{room}", async (HttpContext ctx, string room) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var r = rooms.GetOrAdd(room, _ => new TerminalRoom(room));
    await r.AttachBrowserAsync(ws);
});

app.Run();

sealed class TerminalRoom
{
    public string Name { get; }
    private WebSocket? _agent;
    private readonly SemaphoreSlim _agentSendLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, WebSocket> _browsers = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _browserSendLocks = new();

    public TerminalRoom(string name) { Name = name; }

    public async Task AttachAgentAsync(WebSocket ws)
    {
        if (Interlocked.Exchange(ref _agent, ws) is not null)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "replaced", CancellationToken.None);
        }
        Console.WriteLine($"[{Name}] agent connected");

        var buf = new byte[16384];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buf, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    await BroadcastToBrowsersAsync(buf.AsMemory(0, result.Count));
                }
            }
        }
        finally
        {
            _agent = null;
            Console.WriteLine($"[{Name}] agent disconnected");
        }
    }

    public async Task AttachBrowserAsync(WebSocket ws)
    {
        var id = Guid.NewGuid();
        _browsers[id] = ws;
        _browserSendLocks[id] = new SemaphoreSlim(1, 1);
        Console.WriteLine($"[{Name}] browser {id} connected");

        var buf = new byte[16384];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buf, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
                if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                {
                    await SendToAgentAsync(buf.AsMemory(0, result.Count));
                }
            }
        }
        finally
        {
            _browsers.TryRemove(id, out _);
            _browserSendLocks.TryRemove(id, out _);
            Console.WriteLine($"[{Name}] browser {id} disconnected");
        }
    }

    private async Task SendToAgentAsync(ReadOnlyMemory<byte> data)
    {
        var agent = _agent;
        if (agent is null || agent.State != WebSocketState.Open) return;
        await _agentSendLock.WaitAsync();
        try
        {
            await agent.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
        }
        catch { }
        finally { _agentSendLock.Release(); }
    }

    private async Task BroadcastToBrowsersAsync(ReadOnlyMemory<byte> data)
    {
        foreach (var (id, ws) in _browsers)
        {
            if (ws.State != WebSocketState.Open) continue;
            var lk = _browserSendLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
            await lk.WaitAsync();
            try
            {
                await ws.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch { }
            finally { lk.Release(); }
        }
    }
}