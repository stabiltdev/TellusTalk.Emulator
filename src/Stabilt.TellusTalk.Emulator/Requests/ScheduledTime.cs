using System.Globalization;

namespace Stabilt.TellusTalk.Emulator.Requests;

internal static class ScheduledTime
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd'T'HH:mm",
        "yyyy-MM-dd'T'HH:mmK",
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ssK",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFK",
    ];

    /// <summary>TellusTalk is a Swedish service on Swedish servers; how it reads a time without an offset is unknown.</summary>
    private static readonly TimeZoneInfo Sweden = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    /// <summary>Reads an ISO 8601 date and time; one without an offset is read as Swedish time.</summary>
    public static bool TryParse(string value, out DateTimeOffset due)
    {
        if (!DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            due = default;

            return false;
        }

        if (parsed.Kind == DateTimeKind.Unspecified)
        {
            due = InSweden(parsed);

            return true;
        }

        return DateTimeOffset.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out due);
    }

    /// <summary>
    /// A time with no offset representable in UTC is read as long past, so its message is sent at once. That covers
    /// <c>0001-01-01T00:00:00</c>, which a serializer writes for a <c>DateTime</c> never set, and times before 1879, for
    /// which Linux's time zone data gives Stockholm's local mean time, an offset in seconds that DateTimeOffset refuses.
    /// </summary>
    private static DateTimeOffset InSweden(DateTime local)
    {
        try
        {
            return new DateTimeOffset(local, Sweden.GetUtcOffset(local));
        }
        catch (ArgumentException)
        {
            return DateTimeOffset.MinValue;
        }
    }
}
