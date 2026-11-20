using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Sending.Reading;

/// <summary>
/// Reads a send request the way TellusTalk accepts one: what TellusTalk was seen to refuse and what the emulator cannot
/// record is refused, and what its specification forbids becomes a warning.
/// </summary>
internal static class SendRequestReader
{
    private const string InvalidJson = "TellusTalk Emulator: the request body is not valid JSON.";
    private const string NotAnObject = "TellusTalk Emulator: the request body must be a JSON object.";
    private const string NotAnAddress = "TellusTalk Emulator: 'to' must be an address or a list of addresses.";

    private const string RecipientObjects =
        "TellusTalk Emulator: the emulator handles plain recipients only, not recipient objects. " +
        "TellusTalk itself accepts them.";

    private const string LogRecipient =
        "TellusTalk Emulator: the emulator does not handle log: recipients, which TellusTalk answers with a log_href " +
        "the emulator cannot give. TellusTalk itself accepts them.";

    private const string LogPrefix = "log:";

    /// <summary>
    /// Every channel caught, by its prefix. <c>log:</c> is not caught: TellusTalk answers it with a <c>log_href</c> that
    /// a client waits for.
    /// </summary>
    private static readonly (string Prefix, MessageChannel Channel)[] Prefixes =
    [
        ("sms:", MessageChannel.Sms),
        ("email:", MessageChannel.Email),
        ("fax:", MessageChannel.Fax),
        ("mms:", MessageChannel.Mms),
        ("voice:", MessageChannel.Voice),
        ("group:", MessageChannel.Group),
    ];

    /// <summary>
    /// Text that does not parse is refused as the emulator's limit. The document is disposed before the result returns,
    /// which <see cref="Read(JsonElement)"/> allows for by keeping copies of what it takes from it.
    /// </summary>
    public static ReadResult Read(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return new Refused(InvalidJson);
        }

        using (document)
        {
            return Read(document.RootElement);
        }
    }

    public static ReadResult Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new Refused(NotAnObject);
        }

        var body = root.Deserialize<SendRequestBody>()!;
        if (SendRequestRules.Refusal(body) is { } refusedByTellusTalk)
        {
            return refusedByTellusTalk;
        }

        var (addresses, refusal) = ReadAddresses(body.To);
        if (refusal is not null)
        {
            return new Refused(refusal);
        }

        if (addresses.Any(address => address.StartsWith(LogPrefix, StringComparison.Ordinal)))
        {
            return new Refused(LogRecipient);
        }

        var warnings = new List<Warning>();
        var recipients = addresses
            .Select(address => ReadRecipient(address, warnings))
            .ToList();

        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var field in root.EnumerateObject())
        {
            fields[field.Name] = field.Value.Clone();
        }

        warnings.AddRange(FieldWarnings(fields));

        var request = ReadRequest(recipients, body, fields, warnings);

        return new Accepted(request with
        {
            Warnings = [.. warnings, .. SendRequestRules.Check(request)],
            SmsWarnings = SendRequestRules.CheckSms(request),
        });
    }

    private static SendRequest ReadRequest(
        List<Recipient> recipients,
        SendRequestBody body,
        Dictionary<string, JsonElement> fields,
        List<Warning> warnings)
    {
        var (reportTo, invalidReportTo) = ReadReportAddresses(body.ReportTo, "report_to", warnings);
        var (replyTo, invalidReplyTo) = ReadReportAddresses(body.ReplyTo, "reply_to", warnings);

        return new()
        {
            Recipients = recipients,
            Text = ReadString(body.Text),
            Subject = ReadString(body.Subject),
            Header = ReadString(body.SmsHeader),
            OriginatorSource = ReadString(body.SmsOriginatorSource),
            OriginatorText = ReadString(body.SmsOriginatorText),
            OriginatorNumber = ReadString(body.SmsOriginatorNumber),
            CharacterSet = ReadCharacterSet(body),
            MaxParts = ReadInt(body.SmsMaxParts),
            Udh = ReadString(body.SmsUdh),
            SingleThread = body.SmsSingleThread.ValueKind == JsonValueKind.True,
            ScheduledSendTime = ReadString(body.ScheduledSendTime),
            ReportTo = reportTo,
            InvalidReportTo = invalidReportTo,
            ReplyTo = replyTo,
            InvalidReplyTo = invalidReplyTo,
            Body = body,
            Fields = fields,
            Warnings = warnings,
        };
    }

    /// <summary>
    /// The deprecated <c>sms_format</c> stands in when <c>sms_character_set</c> is absent or null, as
    /// <see cref="SendRequestRules"/> reads it; the rules have refused any value but the four character sets.
    /// </summary>
    private static string? ReadCharacterSet(SendRequestBody body) =>
        ReadString(body.SmsCharacterSet) ?? ReadString(body.SmsFormat);

    /// <summary>The addresses read, and those that could not be, as sent, for the answer to list.</summary>
    private static (List<ReportAddress> Addresses, List<string> Invalid) ReadReportAddresses(
        JsonElement value,
        string name,
        List<Warning> warnings)
    {
        IEnumerable<JsonElement> values = value.ValueKind switch
        {
            JsonValueKind.String => [value],
            JsonValueKind.Array => value.EnumerateArray(),
            _ => [],
        };

        var addresses = new List<ReportAddress>();
        var invalid = new List<string>();
        foreach (var item in values.Where(item => item.ValueKind == JsonValueKind.String))
        {
            var text = item.GetString()!;
            if (ReportAddress.Parse(text) is { } address)
            {
                addresses.Add(address);
            }
            else
            {
                invalid.Add(text);
                warnings.Add(new Warning(
                    WarningCodes.ReportAddressInvalid,
                    name,
                    $"'{text}' is no e-mail address, sms: number or http(s) URL; the emulator leaves it out."));
            }
        }

        return (addresses, invalid);
    }

    /// <summary>K8's rules have refused every other <c>to</c>: it is a string, a list or an object here.</summary>
    private static (IReadOnlyList<string> Addresses, string? Refusal) ReadAddresses(JsonElement to)
    {
        if (to.ValueKind == JsonValueKind.String)
        {
            return ([to.GetString()!], null);
        }

        if (to.ValueKind == JsonValueKind.Object)
        {
            return ([], RecipientObjects);
        }

        var items = to
            .EnumerateArray()
            .ToList();

        if (items.Exists(item => item.ValueKind == JsonValueKind.Object))
        {
            return ([], RecipientObjects);
        }

        if (items.Exists(item => item.ValueKind != JsonValueKind.String))
        {
            return ([], NotAnAddress);
        }

        var addresses = items
            .Select(item => item.GetString()!)
            .ToList();

        return (addresses, null);
    }

    private static Recipient ReadRecipient(string address, List<Warning> warnings)
    {
        foreach (var (prefix, channel) in Prefixes)
        {
            if (address.StartsWith(prefix, StringComparison.Ordinal))
            {
                var rest = address[prefix.Length..];

                return channel switch
                {
                    MessageChannel.Email => ReadEmail(rest, address, warnings),
                    MessageChannel.Group => new Recipient(channel, rest, address),
                    _ => ReadNumber(channel, rest, address, warnings),
                };
            }
        }

        warnings.Add(new Warning(
            WarningCodes.MissingPrefix,
            "to",
            $"'{address}' lacks the 'sms:' prefix the specification asks for; the emulator reads it as an SMS number."));

        return ReadNumber(MessageChannel.Sms, address, address, warnings);
    }

    /// <summary>
    /// That TellusTalk removes spaces and hyphens has been observed in an SMS number, not in another channel's.
    /// </summary>
    private static Recipient ReadNumber(MessageChannel channel, string number, string address, List<Warning> warnings)
    {
        if (number.Contains(' ', StringComparison.Ordinal))
        {
            number = number.Replace(" ", string.Empty, StringComparison.Ordinal);
            warnings.Add(new Warning(
                WarningCodes.SpacesRemoved,
                "to",
                channel == MessageChannel.Sms
                    ? $"'{address}' contains spaces; TellusTalk removes them, and so does the emulator."
                    : $"'{address}' contains spaces; the emulator removes them, as TellusTalk does in an SMS number."));
        }

        if (number.Contains('-', StringComparison.Ordinal))
        {
            number = number.Replace("-", string.Empty, StringComparison.Ordinal);
            warnings.Add(new Warning(
                WarningCodes.HyphensRemoved,
                "to",
                channel == MessageChannel.Sms
                    ? $"'{address}' contains hyphens; TellusTalk removes them, and so does the emulator."
                    : $"'{address}' contains hyphens; the emulator removes them, as TellusTalk does in an SMS " +
                      "number."));
        }

        if (!PhoneNumbers.IsPlusDigits(number))
        {
            warnings.Add(new Warning(
                WarningCodes.NotDigits,
                "to",
                $"'{number}' is not '+' followed by digits, as the specification asks of {NumberKind(channel)} number."));
        }

        return new Recipient(channel, number, address);
    }

    private static string NumberKind(MessageChannel channel) => channel switch
    {
        MessageChannel.Sms => "an SMS",
        MessageChannel.Fax => "a fax",
        MessageChannel.Mms => "an MMS",
        MessageChannel.Voice => "a voice",
        _ => throw new InvalidOperationException($"The channel '{channel}' has no numbers."),
    };

    /// <summary>The specification's pattern for an e-mail address is <c>.+@.+</c>: an '@' with a character each side.</summary>
    private static Recipient ReadEmail(string emailAddress, string address, List<Warning> warnings)
    {
        if (emailAddress.Length < 3 || !emailAddress.AsSpan()[1..^1].Contains('@'))
        {
            warnings.Add(new Warning(
                WarningCodes.EmailInvalid,
                "to",
                $"'{address}' is not a name, '@' and a domain, as the specification asks of an e-mail address."));
        }

        return new Recipient(MessageChannel.Email, emailAddress, address);
    }

    /// <summary>In the order the fields arrived, so the warnings read the way the request does.</summary>
    private static IEnumerable<Warning> FieldWarnings(Dictionary<string, JsonElement> fields)
    {
        foreach (var (name, value) in fields)
        {
            if (!SendRequestBody.Names.Contains(name))
            {
                yield return new Warning(
                    WarningCodes.UnknownField,
                    name,
                    $"'{name}' is not a field TellusTalk's specification defines; TellusTalk accepts it and ignores it.");
            }
            else if (SendRequestRules.Shapes.TryGetValue(name, out var expected) &&
                value.ValueKind != JsonValueKind.Null &&
                !Matches(expected, value))
            {
                yield return new Warning(
                    WarningCodes.WrongJsonType,
                    name,
                    $"'{name}' should be {Description(expected)}; TellusTalk accepts the request, but how it reads the value is unknown.");
            }
        }
    }

    private static bool Matches(JsonShape shape, JsonElement value) => shape switch
    {
        JsonShape.String => value.ValueKind == JsonValueKind.String,
        JsonShape.Number => value.ValueKind == JsonValueKind.Number,
        JsonShape.Boolean => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
        JsonShape.List => value.ValueKind == JsonValueKind.Array,
        _ => value.ValueKind == JsonValueKind.String ||
            (value.ValueKind == JsonValueKind.Array && value
                .EnumerateArray()
                .All(item => item.ValueKind == JsonValueKind.String)),
    };

    private static string Description(JsonShape shape) => shape switch
    {
        JsonShape.String => "a string",
        JsonShape.Number => "a number",
        JsonShape.Boolean => "true or false",
        JsonShape.List => "a list",
        _ => "an address or a list of addresses",
    };

    /// <summary>
    /// A string field of another JSON type is read as its JSON text, the way it would read when printed; a JSON null
    /// is read as absent, since serializers commonly write one for a field they leave out.
    /// </summary>
    private static string? ReadString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        _ => value.GetRawText(),
    };

    private static int? ReadInt(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;
}
