using System.Reflection;

namespace WebSockets.Otp.Abstractions;

public interface IMethodResolver
{
    MethodInfo ResolveHandleMethod(Type endpointType);
}
