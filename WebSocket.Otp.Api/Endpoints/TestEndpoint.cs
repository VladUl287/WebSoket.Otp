using WebSockets.Otp.Abstractions;
using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Endpoints;

namespace WebSockets.Otp.Api.Endpoints;

[WsEndpoint("chat/message/test")]
public sealed class TestEndpoint : WsEndpoint
{
    public override Task HandleAsync(EndpointContext connection)
    {
        throw new NotImplementedException();
    }
}
