using System.Security.Claims;

namespace WebSockets.Otp.Api;

internal static class ClaimsPrincipalUser
{
    private static readonly string idClaim = ClaimTypes.NameIdentifier.ToString();

    internal static T GetUserId<T>(this ClaimsPrincipal? User, IFormatProvider? formatProvider = null)
        where T : struct, IParsable<T>
    {
        var userId = User?.Claims.FirstOrDefault(c => c.Type == idClaim)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return default;
        }

        if (T.TryParse(userId, formatProvider, out T result))
        {
            return result;
        }

        return default;
    }
}