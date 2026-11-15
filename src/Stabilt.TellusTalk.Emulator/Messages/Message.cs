using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>One recipient's message, as the emulator caught it.</summary>
/// <param name="Id">A single recipient's message takes the job's id; each of several gets its own.</param>
/// <param name="JobId">The id the send answered with, shared by every recipient of one request.</param>
/// <param name="DueAt"><c>null</c> for a message sent at once.</param>
/// <param name="Text">What the recipient gets; for an SMS, what the phone shows: the header included and the tracking links shortened.</param>
/// <param name="Sms">Present exactly when <c>To.Channel</c> is <c>Sms</c>.</param>
/// <param name="Warnings">The request's warnings and the message's own.</param>
internal sealed record Message(
    string Id,
    string JobId,
    Recipient To,
    string UserName,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? DueAt,
    string Text,
    SmsContent? Sms,
    SendRequest Request,
    IReadOnlyList<Warning> Warnings,
    string RawRequest)
{
    /// <summary>The id of the earlier message this one duplicates, which held it back; <c>null</c> for a message that went out.</summary>
    public string? DuplicateOf { get; init; }

    /// <summary>A held-back message never falls due, so it is shown from when it arrived.</summary>
    public DateTimeOffset ShownAt => DuplicateOf is null ? DueAt ?? ReceivedAt : ReceivedAt;

    public MessageState StateAt(DateTimeOffset now) =>
        DuplicateOf is not null ? MessageState.Blocked :
        DueAt > now ? MessageState.Scheduled : MessageState.Sent;
}
