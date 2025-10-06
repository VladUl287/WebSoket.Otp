using WebSockets.Otp.Abstractions;
using WebSockets.Otp.AspNet.Extensions;

namespace WebSockets.Otp.Tests;

public sealed class WsEndpointTypeExtensionsTests
{
    [Fact]
    public void Test1()
    {
        var test = typeof(WsEndpoint).IsWsEndpoint();
    }
}
