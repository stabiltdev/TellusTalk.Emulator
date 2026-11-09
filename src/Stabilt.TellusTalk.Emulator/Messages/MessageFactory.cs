using Microsoft.Extensions.Options;
using Stabilt.TellusTalk.Emulator.Ids;
using Stabilt.TellusTalk.Emulator.Parts;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class MessageFactory(TimeProvider time, IOptions<EmulatorOptions> options)
{
    /// <summary>The specification's default for <c>sms_max_parts</c>.</summary>
    private const int DefaultMaxParts = 8;

    public SendJob Create(SendRequest request, string userName, string rawRequest)
    {
        var now = time.GetUtcNow();
        var jobId = Guid.CreateVersion1(now).ToString("N");
        var (text, links) = TrackingLinks.Shorten((request.Header ?? string.Empty) + (request.Text ?? string.Empty));

        var messageWarnings = new List<Warning>();
        var sender = SenderResolver.Resolve(request, options.Value.AccountSender, messageWarnings);

        var characterSet = SmsPartsCalculator.Choose(text, request.CharacterSet);
        if (string.Equals(request.CharacterSet, "gsm", StringComparison.Ordinal) &&
            !text.All(c => GsmAlphabet.IsBasic(c) || GsmAlphabet.IsExtension(c)))
        {
            messageWarnings.Add(new Warning(
                WarningCodes.GsmUnsupportedCharacters,
                "text",
                "The text holds characters outside the GSM alphabet, which sms_character_set gsm forces; how TellusTalk sends them is unknown."));
        }

        var parts = SmsPartsCalculator.Calculate(text, characterSet, request.Udh);
        var maxParts = request.MaxParts is >= 1 and var allowed ? allowed : DefaultMaxParts;
        if (parts.Count > maxParts)
        {
            messageWarnings.Add(new Warning(
                WarningCodes.MaxPartsExceeded,
                "sms_max_parts",
                $"The text needs {parts.Count} parts but sms_max_parts allows {maxParts}; the emulator keeps it whole, and what TellusTalk does is unknown."));
        }

        var dueAt = request.ScheduledSendTime is { } scheduled && ScheduledTime.TryParse(scheduled, out var due) && due > now
            ? due.ToUniversalTime()
            : (DateTimeOffset?)null;

        IReadOnlyList<Warning> warnings = [.. request.Warnings, .. messageWarnings];
        var size = request.Recipients.Count + request.NotCaught.Count;
        var single = size == 1;
        var messages = request.Recipients
            .Select(recipient => new Message(
                single ? jobId : Guid.CreateVersion1(now).ToString("N"),
                jobId,
                recipient,
                userName,
                now,
                dueAt,
                sender,
                text,
                links,
                parts,
                maxParts,
                request,
                warnings,
                rawRequest))
            .ToList();

        return new SendJob(jobId, size, messages);
    }
}
