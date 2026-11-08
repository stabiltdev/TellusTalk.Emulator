using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Requests;

internal enum ReportAddressKind
{
    Email,
    Sms,
    Url,
}

/// <summary>Where TellusTalk would send a delivery report or a reply.</summary>
/// <param name="Target">The e-mail address, the number, or the URL without its settings.</param>
/// <param name="ContentType">The report's format, when the address's settings choose one.</param>
/// <param name="Headers">The headers the address's settings add to the report request.</param>
internal sealed record ReportAddress(
    string AsSent,
    ReportAddressKind Kind,
    string Target,
    string? ContentType,
    IReadOnlyDictionary<string, string> Headers)
{
    private const string EmailPrefix = "email:";
    private const string SmsPrefix = "sms:";
    private const string SettingsEnd = "}://";

    private static readonly string[] Schemes = ["https", "http"];

    /// <summary>
    /// Reads the forms TellusTalk takes: <c>email:</c> or a bare address, <c>sms:+</c> and digits, an absolute URL,
    /// and a URL carrying its settings as <c>https:{…}://host/path</c>. Anything else is <c>null</c>.
    /// </summary>
    public static ReportAddress? Parse(string value)
    {
        if (value.StartsWith(EmailPrefix, StringComparison.Ordinal))
        {
            var email = value[EmailPrefix.Length..];

            return IsEmail(email) ? Simple(value, ReportAddressKind.Email, email) : null;
        }

        if (value.StartsWith(SmsPrefix, StringComparison.Ordinal))
        {
            var number = value[SmsPrefix.Length..];

            return PhoneNumbers.IsPlusDigits(number) ? Simple(value, ReportAddressKind.Sms, number) : null;
        }

        var scheme = Array.Find(Schemes, scheme => value.StartsWith(scheme + ":{", StringComparison.Ordinal));
        if (scheme is not null)
        {
            return WithSettings(value, scheme);
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var url) &&
            Array.Exists(Schemes, scheme => string.Equals(url.Scheme, scheme, StringComparison.Ordinal)))
        {
            return Simple(value, ReportAddressKind.Url, value);
        }

        return !value.Contains(':', StringComparison.Ordinal) && IsEmail(value)
            ? Simple(value, ReportAddressKind.Email, value)
            : null;
    }

    private static ReportAddress? WithSettings(string value, string scheme)
    {
        var settingsStart = scheme.Length + 1;
        var settingsEnd = value.IndexOf(SettingsEnd, settingsStart, StringComparison.Ordinal);
        if (settingsEnd < 0)
        {
            return null;
        }

        var target = $"{scheme}://{value[(settingsEnd + SettingsEnd.Length)..]}";
        if (!Uri.TryCreate(target, UriKind.Absolute, out _))
        {
            return null;
        }

        var settings = value[settingsStart..(settingsEnd + 1)];
        try
        {
            using var document = JsonDocument.Parse(settings);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? contentType = null;
            if (root.TryGetProperty("content_type", out var type))
            {
                if (type.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                contentType = type.GetString();
            }

            var headers = new Dictionary<string, string>(StringComparer.Ordinal);
            if (root.TryGetProperty("headers", out var headerObject))
            {
                if (headerObject.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                foreach (var header in headerObject.EnumerateObject())
                {
                    if (header.Value.ValueKind != JsonValueKind.String)
                    {
                        return null;
                    }

                    headers[header.Name] = header.Value.GetString()!;
                }
            }

            return new ReportAddress(value, ReportAddressKind.Url, target, contentType, headers);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ReportAddress Simple(string value, ReportAddressKind kind, string target) =>
        new(value, kind, target, null, new Dictionary<string, string>(StringComparer.Ordinal));

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@', StringComparison.Ordinal);

        return at > 0 && at < value.Length - 1;
    }
}
