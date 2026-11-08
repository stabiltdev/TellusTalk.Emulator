using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Parts;
using Stabilt.TellusTalk.Emulator.Reports;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>A message as the read API serves it; tests in other repositories depend on this shape.</summary>
internal sealed record MessageResponse(
    string Id,
    string JobId,
    MessageState State,
    string To,
    string ToAsSent,
    string User,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? DueAt,
    SenderResponse Sender,
    string Text,
    PartsResponse Parts,
    IReadOnlyList<AddressResponse> ReportTo,
    IReadOnlyList<AddressResponse> ReplyTo,
    IReadOnlyDictionary<string, JsonElement> Fields,
    IReadOnlyList<WarningResponse> Warnings,
    IReadOnlyList<ReportAttemptResponse> Reports,
    string Request)
{
    public static MessageResponse From(Message message, DateTimeOffset now, IReadOnlyList<ReportAttempt> reports) => new(
        message.Id,
        message.JobId,
        message.StateAt(now),
        message.To.Number,
        message.To.AsSent,
        message.UserName,
        message.ReceivedAt,
        message.DueAt,
        SenderResponse.From(message.Sender),
        message.Text,
        PartsResponse.From(message.Parts, message.MaxParts),
        [.. message.Request.ReportTo.Select(AddressResponse.From)],
        [.. message.Request.ReplyTo.Select(AddressResponse.From)],
        message.Request.Fields,
        [.. message.Warnings.Select(WarningResponse.From)],
        [.. reports.Select(ReportAttemptResponse.From)],
        message.RawRequest);
}

internal sealed record SenderResponse(string Shown, SenderKind Kind, string Source, bool FromAccount)
{
    public static SenderResponse From(ShownSender sender) => new(sender.Display, sender.Kind, sender.Source, sender.FromAccount);
}

internal sealed record PartsResponse(
    SmsCharacterSet CharacterSet,
    int Count,
    int Length,
    IReadOnlyList<int> Starts,
    int LeftInLastPart,
    int MaxParts)
{
    public static PartsResponse From(SmsParts parts, int maxParts) =>
        new(parts.CharacterSet, parts.Count, parts.Length, parts.Starts, parts.LeftInLastPart, maxParts);
}

internal sealed record AddressResponse(
    ReportAddressKind Kind,
    string Target,
    string? ContentType,
    IReadOnlyDictionary<string, string> Headers)
{
    public static AddressResponse From(ReportAddress address) =>
        new(address.Kind, address.Target, address.ContentType, address.Headers);
}

internal sealed record WarningResponse(string Code, string? Field, string Text)
{
    public static WarningResponse From(Warning warning) => new(warning.Code, warning.Field, warning.Text);
}

internal sealed record ReportAttemptResponse(DateTimeOffset At, ReportStatus Status, string Target, bool Sent, string Outcome)
{
    public static ReportAttemptResponse From(ReportAttempt attempt) =>
        new(attempt.At, attempt.Status, attempt.Target, attempt.Sent, attempt.Outcome);
}
