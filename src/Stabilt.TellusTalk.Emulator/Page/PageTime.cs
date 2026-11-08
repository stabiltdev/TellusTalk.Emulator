using System.Globalization;

namespace Stabilt.TellusTalk.Emulator.Page;

/// <summary>A time as the page writes it: ISO 8601 in UTC for the script, which shows it in the browser's local time.</summary>
internal static class PageTime
{
    public static string Iso(DateTimeOffset time) =>
        time.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static string Fallback(DateTimeOffset time) =>
        time.UtcDateTime.ToString("HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
