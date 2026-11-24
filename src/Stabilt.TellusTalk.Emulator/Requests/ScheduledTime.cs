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

    /// <summary>Reads an ISO 8601 date and time; one without an offset is read as UTC, as K8 reads it.</summary>
    public static bool TryParse(string value, out DateTimeOffset due) =>
        DateTimeOffset.TryParseExact(
            value,
            Formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out due);
}
