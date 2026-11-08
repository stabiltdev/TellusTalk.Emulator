using System.Collections.Frozen;
using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>
/// The rules TellusTalk's specification states, in its schema or in a field's description. TellusTalk accepted every
/// request that broke one when probed, so each becomes a warning, never a refusal.
/// </summary>
internal static class SpecificationRules
{
    public const int MaxSenderNameLength = 11;

    public static FrozenSet<string> OriginatorSources { get; } =
        new[] { "replysms", "text", "number", "subject" }.ToFrozenSet(StringComparer.Ordinal);

    public static FrozenSet<string> CharacterSets { get; } =
        new[] { "optimize", "gsm", "unicode", "binary" }.ToFrozenSet(StringComparer.Ordinal);

    public static IReadOnlyList<Warning> Check(SendRequest request) =>
        [.. SenderWarnings(request), .. EncodingWarnings(request), .. OtherWarnings(request)];

    private static IEnumerable<Warning> SenderWarnings(SendRequest request)
    {
        if (request.OriginatorText is { Length: > MaxSenderNameLength } text)
        {
            yield return new Warning(
                WarningCodes.OriginatorTextTooLong,
                "sms_originator_text",
                $"The sender name '{text}' has {text.Length} characters; the specification allows {MaxSenderNameLength}, and TellusTalk passes it on whole.");
        }

        if (IsSource(request, "subject") && request.Subject is { Length: > MaxSenderNameLength } subject)
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

        if (request.OriginatorSource is { } source && !OriginatorSources.Contains(source))
        {
            yield return new Warning(
                WarningCodes.OriginatorSourceInvalid,
                "sms_originator_source",
                $"'{source}' is not a documented sender source; the documented ones are replysms, text, number and subject.");
        }

        if (request.SingleThread && request.OriginatorSource is { } threadSource && !IsSource(request, "replysms"))
        {
            yield return new Warning(
                WarningCodes.SingleThreadWithoutReplySms,
                "sms_single_thread",
                $"sms_single_thread requires the sender source replysms, not '{threadSource}'.");
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

    private static IEnumerable<Warning> OtherWarnings(SendRequest request)
    {
        if (request.MaxParts is < 1)
        {
            yield return new Warning(
                WarningCodes.MaxPartsInvalid,
                "sms_max_parts",
                $"sms_max_parts is {request.MaxParts}; the specification's minimum is 1, so the emulator uses the default of 8.");
        }

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

    private static bool IsSource(SendRequest request, string source) =>
        string.Equals(request.OriginatorSource, source, StringComparison.Ordinal);

    private static bool IsHex(string text) =>
        text.Length % 2 == 0 && text.All(char.IsAsciiHexDigit);
}
