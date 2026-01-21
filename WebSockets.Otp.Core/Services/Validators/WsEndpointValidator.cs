using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Configuration;

namespace WebSockets.Otp.Core.Services.Validators;

public static class WsEndpointValidator
{
    public static WsEndpointAttribute Validate(this WsEndpointAttribute attribute, WsOptions options)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(options);

        var key = attribute.Key;

        ArgumentException.ThrowIfNullOrEmpty(key, "WsEndpoint key cannot be null or empty");

        if (attribute.Key.Length < options.KeyMinLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too short. Minimum length is {options.KeyMinLength}");

        if (attribute.Key.Length > options.KeyMaxLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too long. Maximum length is {options.KeyMaxLength}");

        if (options.KeyPattern is not null && !options.KeyPattern.IsMatch(attribute.Key))
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' does not match the required pattern");

        return attribute;
    }
}
