# WebSockets.Otp

[![NuGet Status](https://img.shields.io/nuget/v/WebSockets.Otp.Abstractions.svg?label=WebSockets.Otp.Abstractions)](https://www.nuget.org/packages/WebSockets.Otp.Abstractions/)
[![NuGet Status](https://img.shields.io/nuget/v/WebSockets.Otp.Core.svg?label=WebSockets.Otp.Core)](https://www.nuget.org/packages/WebSockets.Otp.Core/)
[![NuGet Status](https://img.shields.io/nuget/v/WebSockets.Otp.Redis.svg?label=WebSockets.Otp.Redis)](https://www.nuget.org/packages/WebSockets.Otp.Redis/)

A minimal WebSocket library for ASP.NET Core inspired by REPR principles. Provides a clean endpoint-based API for building real-time applications.

## Quick Start

#### 1. Define your endpoint

```cs
[WsEndpoint("chat/message")]
public class ChatEndpoint : WsEndpoint<ChatMessage, ChatResponse>
{
    public override async Task HandleAsync(ChatMessage request, EndpointContext<ChatResponse> context)
    {
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
builder.Services.AddWsEndpoints();

app.MapEndpoints(
    "/ws",
    (opt) =>
    {
        opt.OnConnected = async (context) =>
        {
            await context.Groups.AddAsync("general-chat", context.ConnectionId);
        };
        opt.OnDisconnected = async (context) =>
        {
            await context.Groups.RemoveAsync("general-chat", context.ConnectionId);
        };
    });
```

## Handshake

The first message sent over a WebSocket connection must be a handshake message. This is required before any endpoint can be invoked.

Client -> Server

```{"protocol":"json"}```

Server -> Client

```{}```

If the first message is not a valid handshake, the server will close the connection. This ensures protocol compatibility and allows for future protocol negotiation.

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
}
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

#### 3. Authorization

Authorization can be applied globally (per connection), per endpoint, or both. Endpoint-level attributes are evaluated in addition to global settings.

**Global (per connection)**

Applied to every endpoint mapped under the same base path:

```cs
app.MapEndpoints(
    "/ws",
    (opt) =>
    {
        opt.AuthorizationData =
        [
            new AuthorizeAttribute
            {
                AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
            }
        ];
    });
```

**Per endpoint**

Applied only to the decorated endpoint:

```cs
[Authorize(Policy = "ws.chat")]
[WsEndpoint("chat/message/send")]
public class ChatEndpoint : WsEndpoint<ChatMessage>
{
    // ...
}
```

**Combined**

Global and endpoint-level authorization are additive a connection must satisfy both to reach the endpoint. Use global rules for connection-wide concerns (e.g. authentication scheme) and per-endpoint attributes for fine-grained policies.

#### 4. Distributed Connections
Scale across multiple server instances by backing the connection registry with a shared store. Requires a Redis instance.

**Install**

```sh
dotnet add package WebSockets.Otp.Redis
```

**Setup**

Register a Redis multiplexer, then enable the Redis-backed connection manager:

```cs
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddRedisManager();
```

## Roadmap

- Performance & memory optimization
- Pre/Post processors
- Rate limiting
- Versioning
