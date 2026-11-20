using System.Collections.Frozen;
using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Sending.Reading;

/// <summary>
/// Every rule about a send request: the shape the specification gives a field, K8's refusals in the order K8 checks
/// them, and the warnings about what the specification forbids and TellusTalk accepts.
/// </summary>
internal static class SendRequestRules
{
    public const int MaxSenderNameLength = 11;

    private const string UnknownSourceSuffix = "; must be one of frozenset({'replysms', 'number', 'text', 'subject'})";

    private static readonly FrozenSet<string> Sources =
        new[] { "replysms", "text", "number", "subject" }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly Func<SendRequestBody, ReadResult?>[] InOrder = [Sender];

    public static FrozenSet<string> CharacterSets { get; } =
        new[] { "optimize", "gsm", "unicode", "binary" }.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>Every field whose shape the schemas fix, by its JSON name.</summary>
    public static FrozenDictionary<string, JsonShape> Shapes { get; } =
        new Dictionary<string, JsonShape>(StringComparer.Ordinal)
        {
            ["text"] = JsonShape.String,
            ["html"] = JsonShape.String,
            ["subject"] = JsonShape.String,
            ["attachments"] = JsonShape.List,
            ["scheduled_send_time"] = JsonShape.String,
            ["billing_id"] = JsonShape.String,
            ["report_to"] = JsonShape.Addresses,
            ["reply_to"] = JsonShape.Addresses,
            ["sms_max_parts"] = JsonShape.Number,
            ["sms_header"] = JsonShape.String,
            ["sms_flash"] = JsonShape.Boolean,
            ["sms_validity_time"] = JsonShape.Number,
            ["sms_originator_text"] = JsonShape.String,
            ["sms_originator_number"] = JsonShape.String,
            ["sms_originator_source"] = JsonShape.String,
            ["sms_single_thread"] = JsonShape.Boolean,
            ["sms_format"] = JsonShape.String,
            ["sms_character_set"] = JsonShape.String,
            ["sms_udh"] = JsonShape.String,
        }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The first rule the body breaks, as K8 answers it, or null.</summary>
    public static ReadResult? Refusal(SendRequestBody body) => InOrder
        .Select(rule => rule(body))
        .FirstOrDefault(result => result is not null);

    /// <summary>The rules about the request as a whole, which every message of it carries.</summary>
    public static IReadOnlyList<Warning> Check(SendRequest request) => [.. RequestWarnings(request)];

    /// <summary>The rules about an SMS, which only the request's SMS messages carry.</summary>
    public static IReadOnlyList<Warning> CheckSms(SendRequest request) =>
        [.. SenderWarnings(request), .. EncodingWarnings(request), .. MaxPartsWarnings(request)];

    private static RefusedByTellusTalk? Sender(SendRequestBody body) =>
        SenderRefusal(body) is { } text ? new RefusedByTellusTalk(text) : null;

    /// <summary>The order between the three sender rules is unobserved.</summary>
    private static string? SenderRefusal(SendRequestBody body)
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
        if (!Sources.Contains(source))
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

    private static IEnumerable<Warning> SenderWarnings(SendRequest request)
    {
        if (request.OriginatorText is { Length: > MaxSenderNameLength } text)
        {
            yield return new Warning(
                WarningCodes.OriginatorTextTooLong,
                "sms_originator_text",
                $"The sender name '{text}' has {text.Length} characters; the specification allows {MaxSenderNameLength}, and TellusTalk passes it on whole.");
        }

        if (SenderResolver.RequestSource(request) is "subject" && request.Subject is { Length: > MaxSenderNameLength } subject)
        {
            yield return new Warning(
                WarningCodes.SubjectTooLong,
                "subject",
                $"The subject '{subject}' is the sender and has {subject.Length} characters; the specification allows {MaxSenderNameLength}.");
        }

        if (request.OriginatorNumber is { } number && !PhoneNumbers.IsPlusDigits(number))
        {
            yield return new Warning(
                WarningCodes.OriginatorNumberInvalid,
                "sms_originator_number",
                $"The sender number '{number}' is not '+' followed by digits, as the specification asks.");
        }
    }

    private static IEnumerable<Warning> EncodingWarnings(SendRequest request)
    {
        if (request.Body.SmsFormat.ValueKind != JsonValueKind.Undefined)
        {
            yield return new Warning(
                WarningCodes.FormatDeprecated,
                "sms_format",
                "sms_format is deprecated; the specification asks for sms_character_set instead.");
        }

        if (request.CharacterSet is { } characterSet && !CharacterSets.Contains(characterSet))
        {
            yield return new Warning(
                WarningCodes.CharacterSetInvalid,
                request.Body.SmsCharacterSet.ValueKind != JsonValueKind.Undefined ? "sms_character_set" : "sms_format",
                $"'{characterSet}' is not a documented character set; the emulator counts the text as optimize.");
        }

        if (!string.Equals(request.CharacterSet, "binary", StringComparison.Ordinal))
        {
            yield break;
        }

        if (request.Udh is null)
        {
            yield return new Warning(
                WarningCodes.BinaryWithoutUdh,
                "sms_udh",
                "A binary SMS must be paired with sms_udh, the specification says.");
        }

        if (!IsHex(request.Text ?? string.Empty))
        {
            yield return new Warning(
                WarningCodes.BinaryNotHex,
                "text",
                "A binary SMS's text must be hex-encoded bytes.");
        }
    }

    private static IEnumerable<Warning> MaxPartsWarnings(SendRequest request)
    {
        if (request.MaxParts is < 1)
        {
            yield return new Warning(
                WarningCodes.MaxPartsInvalid,
                "sms_max_parts",
                $"sms_max_parts is {request.MaxParts}; the specification's minimum is 1, so the emulator uses the default of 8.");
        }
    }

    private static IEnumerable<Warning> RequestWarnings(SendRequest request)
    {
        var billingId = request.Body.BillingId;
        if (billingId.ValueKind == JsonValueKind.String &&
            !billingId.GetString()!.All(char.IsAsciiLetterOrDigit))
        {
            yield return new Warning(
                WarningCodes.BillingIdInvalid,
                "billing_id",
                $"The billing id '{billingId.GetString()}' is not alphanumeric, as the specification asks.");
        }

        if (request.ScheduledSendTime is { } scheduled && !ScheduledTime.TryParse(scheduled, out _))
        {
            yield return new Warning(
                WarningCodes.ScheduledTimeInvalid,
                "scheduled_send_time",
                $"'{scheduled}' is not an ISO 8601 date and time; the emulator sends the message at once.");
        }
    }

    private static bool IsHex(string text) =>
        text.Length % 2 == 0 && text.All(char.IsAsciiHexDigit);
}

internal enum JsonShape
{
    String,
    Number,
    Boolean,
    List,
    Addresses,
}
