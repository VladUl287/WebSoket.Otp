using System.Text.RegularExpressions;
using WebSockets.Otp.Abstractions.Attributes;

namespace WebSockets.Otp.AspNet.Validators;

public sealed class WsEndpointAttributeOptions
{
    public readonly static WsEndpointAttributeOptions Default = new();

    public int MaxKeyLength { get; init; } = 256;
    public int MinKeyLength { get; init; } = 1;
    public Regex? KeyPattern { get; init; }
}

public static class WsEndpointValidator
{
    public static WsEndpointAttribute Validate(this WsEndpointAttribute? attribute, WsEndpointAttributeOptions options)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        ArgumentNullException.ThrowIfNull(options);

        var key = attribute.Key;

        ArgumentException.ThrowIfNullOrEmpty(key, "WsEndpoint key cannot be null or empty");

        if (attribute.Key.Length < options.MinKeyLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too short. Minimum length is {options.MinKeyLength}");

        if (attribute.Key.Length > options.MaxKeyLength)
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' is too long. Maximum length is {options.MaxKeyLength}");

        if (options.KeyPattern is not null && !options.KeyPattern.IsMatch(attribute.Key))
            throw new InvalidOperationException($"WsEndpoint key '{attribute.Key}' does not match the required pattern");

        return attribute;
    }
}
