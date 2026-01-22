using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services.Validators;

public static class WsEndpointValidator
{
    public static WsEndpointAttribute Validate(this WsEndpointAttribute attribute, WsGlobalOptions options)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(options);

        var key = attribute.Key;

        ArgumentException.ThrowIfNullOrEmpty(key, "WsEndpoint key cannot be null or empty");

        if (attribute.Key.Length < options.Keys.MinLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too short. Minimum length is {options.Keys.MinLength}");

        if (attribute.Key.Length > options.Keys.MaxLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too long. Maximum length is {options.Keys.MaxLength}");

        if (options.Keys.Pattern is not null && !options.Keys.Pattern.IsMatch(attribute.Key))
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' does not match the required pattern");

        return attribute;
    }
}
