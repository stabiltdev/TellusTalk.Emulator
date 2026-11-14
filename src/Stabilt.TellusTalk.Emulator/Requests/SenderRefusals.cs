using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>
/// The sender rules K8 was seen to enforce, with its texts word for word. The order between the three rules is
/// unobserved.
/// </summary>
internal static class SenderRefusals
{
    private const string UnknownSourceSuffix = "; must be one of frozenset({'replysms', 'number', 'text', 'subject'})";

    /// <summary>The first rule the body breaks, as K8 words it, or null.</summary>
    public static string? Find(SendRequestBody body)
    {
        var typed = new (string Name, JsonElement Value)[]
        {
            ("subject", body.Subject),
            ("sms_originator_source", body.SmsOriginatorSource),
            ("sms_originator_text", body.SmsOriginatorText),
            ("sms_originator_number", body.SmsOriginatorNumber),
        };

        foreach (var (name, value) in typed)
        {
            if (value.ValueKind is not (JsonValueKind.Undefined or JsonValueKind.String))
            {
                return $"Invalid {name} not a string: {PythonText.Of(value)}";
            }
        }

        if (body.SmsOriginatorSource.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var source = body.SmsOriginatorSource.GetString()!;
        if (!SpecificationRules.OriginatorSources.Contains(source))
        {
            return $"Invalid sms_originator_source: {source}{UnknownSourceSuffix}";
        }

        return source switch
        {
            "subject" => Missing(body.Subject, "subject", "non-empty string", "."),
            "text" => Missing(body.SmsOriginatorText, "sms_originator_text", "string", string.Empty),
            "number" => Missing(body.SmsOriginatorNumber, "sms_originator_number", "number", string.Empty),
            _ => null,
        };
    }

    private static string? Missing(JsonElement value, string field, string expected, string ending) =>
        value.ValueKind == JsonValueKind.Undefined
            ? $"400 Bad Request: Missing/invalid '{field}'. Expected {expected}, got 'None'{ending}"
            : value.GetString()!.Length == 0
                ? $"400 Bad Request: Missing/invalid '{field}'. Expected {expected}, got ''{ending}"
                : null;
}
