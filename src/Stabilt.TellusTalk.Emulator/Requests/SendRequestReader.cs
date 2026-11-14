using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>
/// Reads a send request the way TellusTalk accepts one: what TellusTalk was seen to refuse and what the emulator cannot
/// record is refused, and what its specification forbids becomes a warning.
/// </summary>
internal static class SendRequestReader
{
    private const string NotAnObject = "TellusTalk Emulator: the request body must be a JSON object.";
    private const string NoRecipient = "TellusTalk Emulator: a request needs 'to' for the emulator to record a message.";
    private const string NotAnAddress = "TellusTalk Emulator: 'to' must be an address or a list of addresses.";

    private const string RecipientObjects =
        "TellusTalk Emulator: the emulator handles plain sms: recipients only, not recipient objects. " +
        "TellusTalk itself accepts them.";

    private const string LogRecipient =
        "TellusTalk Emulator: the emulator does not handle log: recipients, which TellusTalk answers with a log_href " +
        "the emulator cannot give. TellusTalk itself accepts them.";

    private const string SmsPrefix = "sms:";

    private const string LogPrefix = "log:";

    /// <summary>
    /// Channels whose recipients are counted and named but not caught. <c>log:</c> is not one: TellusTalk answers it with
    /// a <c>log_href</c> that a client waits for.
    /// </summary>
    private static readonly string[] OtherChannels = ["email:", "fax:", "mms:", "voice:", "group:"];

    public static ReadResult Read(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new Refused(NotAnObject);
        }

        var body = root.Deserialize<SendRequestBody>()!;
        if (SenderRefusals.Find(body) is { } senderRefusal)
        {
            return new RefusedByTellusTalk(senderRefusal);
        }

        if (body.To.ValueKind == JsonValueKind.Undefined)
        {
            return new Refused(NoRecipient);
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

        var notCaught = addresses
            .Where(address => OtherChannel(address) is not null)
            .ToList();

        var smsAddresses = addresses
            .Where(address => OtherChannel(address) is null)
            .ToList();

        if (smsAddresses.Count == 0 && notCaught.Count > 0)
        {
            return new Refused(
                $"TellusTalk Emulator: the emulator catches sms: recipients only, and the request has none, only '{OtherChannel(notCaught[0])}'. TellusTalk itself accepts it.");
        }

        var warnings = new List<Warning>();
        var recipients = smsAddresses
            .Select(address => ReadRecipient(address, warnings))
            .ToList();

        var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var field in root.EnumerateObject())
        {
            fields[field.Name] = field.Value.Clone();
        }

        warnings.AddRange(FieldWarnings(fields));

        var request = ReadRequest(recipients, notCaught, body, fields, warnings);

        return new Accepted(request with { Warnings = [.. warnings, .. SpecificationRules.Check(request)] });
    }

    private static string? OtherChannel(string address) =>
        Array.Find(OtherChannels, channel => address.StartsWith(channel, StringComparison.Ordinal));

    private static SendRequest ReadRequest(
        List<Recipient> recipients,
        List<string> notCaught,
        SendRequestBody body,
        Dictionary<string, JsonElement> fields,
        List<Warning> warnings) =>
        new()
        {
            Recipients = recipients,
            NotCaught = notCaught,
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
            ReportTo = ReadReportAddresses(body.ReportTo, "report_to", warnings),
            ReplyTo = ReadReportAddresses(body.ReplyTo, "reply_to", warnings),
            Body = body,
            Fields = fields,
            Warnings = warnings,
        };

    /// <summary>The deprecated <c>sms_format</c> stands in when <c>sms_character_set</c> is absent and its value is a documented one.</summary>
    private static string? ReadCharacterSet(SendRequestBody body)
    {
        if (ReadString(body.SmsCharacterSet) is { } characterSet)
        {
            return characterSet;
        }

        return ReadString(body.SmsFormat) is { } format && SpecificationRules.CharacterSets.Contains(format)
            ? format
            : null;
    }

    private static List<ReportAddress> ReadReportAddresses(JsonElement value, string name, List<Warning> warnings)
    {
        IEnumerable<JsonElement> values = value.ValueKind switch
        {
            JsonValueKind.String => [value],
            JsonValueKind.Array => value.EnumerateArray(),
            _ => [],
        };

        var addresses = new List<ReportAddress>();
        foreach (var item in values.Where(item => item.ValueKind == JsonValueKind.String))
        {
            var text = item.GetString()!;
            if (ReportAddress.Parse(text) is { } address)
            {
                addresses.Add(address);
            }
            else
            {
                warnings.Add(new Warning(
                    WarningCodes.ReportAddressInvalid,
                    name,
                    $"'{text}' is no e-mail address, sms: number or http(s) URL; the emulator leaves it out."));
            }
        }

        return addresses;
    }

    private static (IReadOnlyList<string> Addresses, string? Refusal) ReadAddresses(JsonElement to)
    {
        if (to.ValueKind == JsonValueKind.String)
        {
            return ([to.GetString()!], null);
        }

        if (to.ValueKind != JsonValueKind.Array)
        {
            return ([], NotAnAddress);
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
        var number = address;
        if (address.StartsWith(SmsPrefix, StringComparison.Ordinal))
        {
            number = address[SmsPrefix.Length..];
        }
        else
        {
            warnings.Add(new Warning(
                WarningCodes.MissingPrefix,
                "to",
                $"'{address}' lacks the 'sms:' prefix the specification asks for; the emulator reads it as an SMS number."));
        }

        if (number.Contains(' ', StringComparison.Ordinal))
        {
            number = number.Replace(" ", string.Empty, StringComparison.Ordinal);
            warnings.Add(new Warning(
                WarningCodes.SpacesRemoved,
                "to",
                $"'{address}' contains spaces; TellusTalk removes them, and so does the emulator."));
        }

        if (!PhoneNumbers.IsPlusDigits(number))
        {
            warnings.Add(new Warning(
                WarningCodes.NotDigits,
                "to",
                $"'{number}' is not '+' followed by digits, as the specification asks of an SMS number."));
        }

        return new Recipient(number, address);
    }

    /// <summary>In the order the fields arrived, so the warnings read the way the request does.</summary>
    private static IEnumerable<Warning> FieldWarnings(Dictionary<string, JsonElement> fields)
    {
        foreach (var (name, value) in fields)
        {
            if (!SendRequestBody.Known.TryGetValue(name, out var shape))
            {
                yield return new Warning(
                    WarningCodes.UnknownField,
                    name,
                    $"'{name}' is not a field TellusTalk's specification defines; TellusTalk accepts it and ignores it.");
            }
            else if (shape is { } expected && value.ValueKind != JsonValueKind.Null && !Matches(expected, value))
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
