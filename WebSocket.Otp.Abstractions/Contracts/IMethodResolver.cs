using System.Reflection;

namespace WebSockets.Otp.Abstractions.Contracts;

public interface IMethodResolver
{
    MethodInfo ResolveHandleMethod(Type endpointType);
}
