using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.AspNet.Extensions;

public static class WsMiddlewareOptionsExtensions
{
    public static void Validate(this WsMiddlewareOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ArgumentNullException.ThrowIfNull(options.Paths, nameof(options.Paths));

        if (options.Paths.RequestPath == PathString.Empty)
            throw new ValidationException("Request path cannot be empty");

        if (options.Paths.HandshakePath == PathString.Empty)
            throw new ValidationException("Handshake path cannot be empty");

        if (options.Paths.RequestPath == options.Paths.HandshakePath)
            throw new ValidationException("Request path and handshake path cannot be the same");

        ArgumentNullException.ThrowIfNull(options.Memory, nameof(options.Memory));

        if (options.Memory.MaxMessageSize <= 0)
            throw new ValidationException("Max message size must be positive");

        if (options.Memory.InitialBufferSize <= 0)
            throw new ValidationException("Initial buffer size must be positive");

        if (options.Memory.InitialBufferSize > options.Memory.MaxMessageSize)
            throw new ValidationException("Initial buffer size cannot exceed max message size");

        if (options.Memory.MaxBufferPoolSize <= 0)
            throw new ValidationException("Buffer pool size must be positive");

        ArgumentNullException.ThrowIfNull(options.Processing, nameof(options.Processing));
        ArgumentNullException.ThrowIfNull(options.Processing.Mode, nameof(options.Processing.Mode));

        if (options.Processing.Mode == ProcessingMode.Parallel && options.Processing.MaxParallelOperations <= 0)
            throw new ValidationException("Max parallel operations must be positive in parallel mode");

        ArgumentNullException.ThrowIfNull(options.Authorization, nameof(options.Authorization));
        ArgumentNullException.ThrowIfNull(options.Authorization.Schemes, nameof(options.Authorization.Schemes));
        ArgumentNullException.ThrowIfNull(options.Authorization.Policies, nameof(options.Authorization.Policies));
        ArgumentNullException.ThrowIfNull(options.Authorization.Roles, nameof(options.Authorization.Roles));

        ArgumentNullException.ThrowIfNull(options.Connection, nameof(options.Connection));
    }
}
