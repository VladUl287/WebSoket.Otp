# WebSockets.Otp

A minimal WebSocket library for ASP.NET Core inspired by FastEndpoints architecture. Provides clean endpoint-based API for building real-time applications.

## Quick Start

#### 1. Define your endpoint

```cs
[WsEndpoint("chat/message")]
public class ChatEndpoint : WsEndpoint<ChatMessage, ChatResponse>
{
    public override async Task HandleAsync(ChatMessage request, EndpointContext<ChatResponse> context)
    {
        // Broadcast message to group
        await context.Send
            .Group("general-chat")
            .SendAsync(new ChatResponse
            {
                Username = request.Username,
                Message = request.Message,
                Timestamp = DateTime.UtcNow
            }, default);
    }
}

public class ChatMessage
{
    public string Username { get; set; }
    public string Message { get; set; }
}

public class ChatResponse
{
    public string Username { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### 2. Configure services

```cs
// Program.cs
builder.Services.AddWsEndpoints(options =>
{
    options.Keys.CaseSensitive = false;
    options.BufferPoolSize = 1000;
    options.ReceiveBufferSize = 4096;
});

// Optional: Configure JSON serialization
builder.Services.AddJsonSerializer(options =>
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
```

#### 3. Map WebSocket endpoints
   
```cs
app.MapEndpoints(
    "/ws",
    (opt) =>
    {
        opt.OnConnected = async (context) =>
        {
            var userId = context.Context.User.GetUserId<long>();
            await context.Groups.AddAsync(userId.ToString(), context.ConnectionId);
        };
        opt.OnDisconnected = async (context) =>
        {
            var userId = context.Context.User.GetUserId<long>();
            await context.Groups.RemoveAsync(userId.ToString(), context.ConnectionId);
        };
    });
```

## Endpoint Types

The library supports three endpoint patterns:

#### 1. Simple Endpoint (No request/response)

```cs
[WsEndpoint("system/status")]
public class SystemStatusEndpoint : WsEndpoint
{
    public override async Task HandleAsync(EndpointContext context)
    {
        // Handle raw WebSocket messages
        var buffer = context.Payload.Span;
        // Custom processing logic
    }
}
```

#### 2. Request-only Endpoint (Any type response)

```cs
[WsEndpoint("notifications/subscribe")]
public class SubscribeEndpoint : WsEndpoint<SubscribeRequest>
{
    public override async Task HandleAsync(SubscribeRequest request, EndpointContext context)
    {
        await context.Groups.AddAsync("notifications", context.ConnectionId);
        await connection.Send
            .SendAsync(new
            {
                Data = "response"
            }, default);
    }
}
```

#### 3. Request/Response Endpoint

```cs
[WsEndpoint("calculator/add")]
public class AddEndpoint : WsEndpoint<AddRequest, AddResponse>
{
    public override async Task HandleAsync(AddRequest request, EndpointContext<AddResponse> context)
    {
        var result = request.A + request.B;
        await context.Send
            .Client(context.ConnectionId)
            .SendAsync(new AddResponse
            {
                Result = result,
                Operation = "addition"
            }, default);
    }
}
```

## Advanced Features

#### 1. Dependency Injection and lifetime management

```cs
[WsEndpoint("auth/validate", ServiceLifetime.Singleton)]
public class AuthEndpoint : WsEndpoint<AuthRequest, AuthResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthEndpoint> _logger;

    public AuthEndpoint(IAuthService authService, ILogger<AuthEndpoint> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    public override async Task HandleAsync(AuthRequest request, EndpointContext<AuthResponse> context)
    {
        var isValid = await _authService.ValidateAsync(request.Token);
    }
}s
```

#### 2. Group Management

```cs
public class ChatEndpoint : WsEndpoint<ChatMessage>
{
    public override async Task HandleAsync(ChatMessage request, EndpointContext context)
    {
        // Add connection to group
        await context.Groups.AddAsync("chat-room", context.ConnectionId);

        // Send to specific group
        await context.Send
            .Group("chat-room")
            .SendAsync(new { Message = "Welcome!" });

        // Send to multiple groups
        await context.Send
            .Group("chat-room")
            .Group("custom")
            .SendAsync(new { Message = "Welcome!" });

        // Remove from group
        await context.Groups.RemoveAsync("chat-room", context.ConnectionId);
    }
}
```

## Limitations

* Pre-alpha: API may change
* No built-in reconnection handling: Client must handle reconnection
* No built-in scaling: Single-server by default (yet)

## Roadmap

...