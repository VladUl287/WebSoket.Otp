using WebSockets.Otp.Abstractions.Attributes;
using WebSockets.Otp.Abstractions.Options;

namespace WebSockets.Otp.Core.Services.Validators;

public static class WsEndpointValidator
{
    public static WsEndpointAttribute Validate(this WsEndpointAttribute attribute, WsEndpointKeyOptions options)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(options);

        var key = attribute.Key;

        ArgumentException.ThrowIfNullOrEmpty(key, "WsEndpoint key cannot be null or empty");

        if (attribute.Key.Length < options.MinLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too short. Minimum length is {options.MinLength}");

        if (attribute.Key.Length > options.MaxLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too long. Maximum length is {options.MaxLength}");

        if (options.Pattern is not null && !options.Pattern.IsMatch(attribute.Key))
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' does not match the required pattern");

        return attribute;
    }
}
