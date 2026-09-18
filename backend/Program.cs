using System.Collections.Concurrent;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var rooms = new ConcurrentDictionary<string, Room>();

app.Map("/ws/agent/{room}", async (HttpContext ctx, string room) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var r = rooms.GetOrAdd(room, _ => new Room(room));
    await r.AttachAgentAsync(ws);
});

app.Map("/ws/terminal/{room}", async (HttpContext ctx, string room) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest) { ctx.Response.StatusCode = 400; return; }
    var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var r = rooms.GetOrAdd(room, _ => new Room(room));
    await r.AttachBrowserAsync(ws);
});

app.Run();

sealed class Room
{
    public string Name { get; }
    private WebSocket? _agent;
    private readonly SemaphoreSlim _agentSendLock = new(1, 1);
    private readonly ConcurrentDictionary<Guid, WebSocket> _browsers = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _browserSendLocks = new();

    public Room(string name) { Name = name; }

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