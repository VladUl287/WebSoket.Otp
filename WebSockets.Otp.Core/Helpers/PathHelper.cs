namespace WebSockets.Otp.Core.Helpers;

public static class PathHelper
{
    public static bool IsValid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return false;
    }

    public static string Normilize(string input)
    {
        const string score = "/";

        if (string.IsNullOrWhiteSpace(input))
            return score;

        if (!Uri.TryCreate(input, UriKind.Relative, out var uri))
            return score;

        var trimmed = uri.AbsolutePath.Trim('/', ' ');
        if (string.IsNullOrEmpty(trimmed))
            return score;

        return trimmed;
    }
}
