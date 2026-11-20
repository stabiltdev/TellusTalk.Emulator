using System.Collections.Frozen;
using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Sending.Reading;

/// <summary>
/// Every rule about a send request: the shape the specification gives a field; K8's refusals and server errors, in the
/// order K8 was seen to check them and elsewhere in one that keeps every order seen; and the warnings about what the
/// specification forbids and TellusTalk accepts.
/// </summary>
internal static class SendRequestRules
{
    public const int MaxSenderNameLength = 11;

    private const string UnknownSourceSuffix = "; must be one of frozenset({'replysms', 'number', 'text', 'subject'})";

    private const string UnknownCharacterSetSuffix =
        "; must be one of frozenset({'optimize', 'unicode', 'binary', 'gsm'})";

    private static readonly FrozenSet<string> Sources =
        new[] { "replysms", "text", "number", "subject" }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly FrozenSet<string> CharacterSets =
        new[] { "optimize", "gsm", "unicode", "binary" }.ToFrozenSet(StringComparer.Ordinal);

    private static readonly Func<SendRequestBody, ReadResult?>[] InOrder =
    [
        To,
        Text,
        Attachments,
        ReplyTo,
        ReportTo,
        ValidityTime,
        MaxParts,
        CharacterSet,
        Header,
        Sender,
        BillingId,
    ];

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

    /// <summary>
    /// K8's "missing or empty", here and for <c>text</c>, is read as Python's truth test run before the type, so
    /// <c>0</c> and <c>false</c> are taken as missing; only null, absence and, in <c>text</c>, <c>""</c> were seen.
    /// </summary>
    private static RefusedByTellusTalk? To(SendRequestBody body)
    {
        if (PythonText.IsFalse(body.To))
        {
            return new RefusedByTellusTalk("key: to is missing or empty");
        }

        return body.To.ValueKind is JsonValueKind.String or JsonValueKind.Array or JsonValueKind.Object
            ? null
            : new RefusedByTellusTalk(
                $"key: to is of type <class '{PythonText.TypeName(body.To)}'> expected (str,dict,list)");
    }

    private static RefusedByTellusTalk? Text(SendRequestBody body)
    {
        if (PythonText.IsFalse(body.Text))
        {
            return new RefusedByTellusTalk("key: text is missing or empty");
        }

        return body.Text.ValueKind == JsonValueKind.String
            ? null
            : new RefusedByTellusTalk($"key: text is of type {PythonText.TypeName(body.Text)} expected (str)");
    }

    /// <summary>
    /// K8's two texts, for <c>null</c> and <c>"file"</c>, are read as Python iterating <c>attachments</c> and calling
    /// <c>pop</c> on each item, so what cannot be iterated, or an item without <c>pop</c>, is taken to fail with Python's
    /// own error: a string's items are its characters, an object's its keys.
    /// </summary>
    private static FailedInTellusTalk? Attachments(SendRequestBody body)
    {
        var attachments = body.Attachments;

        return attachments.ValueKind switch
        {
            JsonValueKind.Undefined => null,
            JsonValueKind.String or JsonValueKind.Object => PythonText.IsFalse(attachments) ? null : NoPop("str"),
            JsonValueKind.Array => attachments
                .EnumerateArray()
                .Where(item => item.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
                .Select(item => NoPop(PythonText.TypeName(item)))
                .FirstOrDefault(),
            _ => new FailedInTellusTalk($"'{PythonText.TypeName(attachments)}' object is not iterable"),
        };
    }

    private static FailedInTellusTalk NoPop(string type) => new($"'{type}' object has no attribute 'pop'");

    private static RefusedByTellusTalk? ReplyTo(SendRequestBody body) => AddressList(body.ReplyTo, "reply_to");

    private static RefusedByTellusTalk? ReportTo(SendRequestBody body) => AddressList(body.ReportTo, "report_to");

    private static RefusedByTellusTalk? AddressList(JsonElement value, string field) =>
        value.ValueKind is JsonValueKind.Undefined or JsonValueKind.String or JsonValueKind.Array or JsonValueKind.Object
            ? null
            : new RefusedByTellusTalk(
                $"400 Bad Request: {field} is of type <class '{PythonText.TypeName(value)}'>. Expected (list,dict,str)");

    private static RefusedByTellusTalk? ValidityTime(SendRequestBody body) =>
        NullCount(body.SmsValidityTime, "sms_validity_time");

    private static RefusedByTellusTalk? MaxParts(SendRequestBody body) => NullCount(body.SmsMaxParts, "sms_max_parts");

    /// <summary>Only null was seen refused; K8 took <c>"2"</c> and <c>"60"</c>, so what else it refuses is not guessed.</summary>
    private static RefusedByTellusTalk? NullCount(JsonElement value, string field) =>
        value.ValueKind == JsonValueKind.Null ? new RefusedByTellusTalk($"Invalid {field} not an int: None") : null;

    /// <summary>
    /// K8 was seen to check the deprecated <c>sms_format</c> in place of an absent <c>sms_character_set</c>, naming it
    /// <c>sms_character_set</c> all the same. That it does so for a null one too, and that a binary SMS's <c>sms_udh</c>
    /// meets the truth test, are readings.
    /// </summary>
    private static RefusedByTellusTalk? CharacterSet(SendRequestBody body)
    {
        var value = body.SmsCharacterSet.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? body.SmsFormat
            : body.SmsCharacterSet;

        if (value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            return new RefusedByTellusTalk($"Invalid sms_character_set not a string: {PythonText.Of(value)}");
        }

        var characterSet = value.GetString()!;
        if (!CharacterSets.Contains(characterSet))
        {
            return new RefusedByTellusTalk($"Invalid sms_character_set: {characterSet}{UnknownCharacterSetSuffix}");
        }

        return string.Equals(characterSet, "binary", StringComparison.Ordinal) && PythonText.IsFalse(body.SmsUdh)
            ? new RefusedByTellusTalk("400 Bad Request: Binary sms_format missing sms_udh")
            : null;
    }

    private static RefusedByTellusTalk? Header(SendRequestBody body) =>
        body.SmsHeader.ValueKind is JsonValueKind.Undefined or JsonValueKind.String
            ? null
            : new RefusedByTellusTalk($"Invalid sms_header not a string: {PythonText.Of(body.SmsHeader)}");

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

    /// <summary>
    /// K8 was seen to refuse a space and a hyphen; the specification's "alphanumeric" is read as ASCII letters and
    /// digits, and an empty id passes.
    /// </summary>
    private static RefusedByTellusTalk? BillingId(SendRequestBody body) =>
        body.BillingId.ValueKind == JsonValueKind.String && !body.BillingId.GetString()!.All(char.IsAsciiLetterOrDigit)
            ? new RefusedByTellusTalk($"Invalid billing_id {body.BillingId.GetString()}")
            : null;

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

        if (string.Equals(request.CharacterSet, "binary", StringComparison.Ordinal) && !IsHex(request.Text ?? string.Empty))
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
