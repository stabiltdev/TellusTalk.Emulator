using Stabilt.TellusTalk.Emulator.Parts;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>One recipient's SMS, as the emulator caught it.</summary>
/// <param name="Id">A single recipient's message takes the job's id; each of several gets its own.</param>
/// <param name="JobId">The id the send answered with, shared by every recipient of one request.</param>
/// <param name="DueAt"><c>null</c> for a message sent at once.</param>
/// <param name="Text">What the phone shows: the header included and the tracking links shortened.</param>
/// <param name="Warnings">The request's warnings and the message's own.</param>
internal sealed record Message(
    string Id,
    string JobId,
    Recipient To,
    string UserName,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? DueAt,
    ShownSender Sender,
    string Text,
    IReadOnlyList<TrackingLink> Links,
    SmsParts Parts,
    int MaxParts,
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
