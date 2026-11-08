using System.Text;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal static class BasicCredentials
{
    /// <summary>The user name in HTTP Basic credentials, or <c>null</c> when the header is missing or malformed.</summary>
    public static string? UserName(string? authorization)
    {
        if (authorization is null)
        {
            return null;
        }

        var space = authorization.IndexOf(' ', StringComparison.Ordinal);
        if (space < 0 || !string.Equals(authorization[..space], "Basic", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string credentials;
        try
        {
            credentials = Encoding.UTF8.GetString(Convert.FromBase64String(authorization[(space + 1)..].Trim()));
        }
        catch (FormatException)
        {
            return null;
        }

        var colon = credentials.IndexOf(':', StringComparison.Ordinal);

        return colon > 0 ? credentials[..colon] : null;
    }
}
