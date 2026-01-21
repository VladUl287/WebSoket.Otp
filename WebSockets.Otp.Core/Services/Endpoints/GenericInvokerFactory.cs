using WebSockets.Otp.Core.Extensions;
using WebSockets.Otp.Abstractions.Endpoints;
using WebSockets.Otp.Core.Services.Endpoints.Generic;

namespace WebSockets.Otp.Core.Services.Endpoints;

public sealed class GenericInvokerFactory : IEndpointInvokerFactory
{
    public IEndpointInvoker Create(Type endpointType) => CreateInvokerViaReflection(endpointType);

    private static IEndpointInvoker CreateInvokerViaReflection(Type endpointType)
    {
        var baseType = endpointType.GetBaseEndpointType() ??
            throw new NotSupportedException($"Type {endpointType} does not inherit from WsEndpoint");

        if (baseType.IsGenericType)
        {
            var genericArgs = baseType.GetGenericArguments();

            if (genericArgs.Length == 2)
            {
                var invokerType = typeof(RequestResponseEndpointInvoker<,>).MakeGenericType(genericArgs[0], genericArgs[1]);
                return (IEndpointInvoker)Activator.CreateInstance(invokerType)!;
            }

            if (genericArgs.Length == 1)
            {
                var invokerType = typeof(RequestEndpointInvoker<>).MakeGenericType(genericArgs[0]);
                return (IEndpointInvoker)Activator.CreateInstance(invokerType)!;
            }
        }

        return new EmptyEndpointInvoker();
    }
}
