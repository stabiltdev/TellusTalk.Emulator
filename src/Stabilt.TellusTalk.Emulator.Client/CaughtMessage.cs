using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Client;

/// <summary>A message the emulator caught, as its read API serves it.</summary>
public sealed record CaughtMessage
{
    public string Id { get; init; } = string.Empty;

    public string JobId { get; init; } = string.Empty;

    public MessageState State { get; init; }

    public string? DuplicateOf { get; init; }

    /// <summary>Why the message was not sent; <c>null</c> for a message that went out.</summary>
    public BlockedReason? BlockedReason { get; init; }

    public MessageChannel Channel { get; init; }

    public string To { get; init; } = string.Empty;

    public string ToAsSent { get; init; } = string.Empty;

    public string User { get; init; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; init; }

    public DateTimeOffset? DueAt { get; init; }

    /// <summary><c>null</c> for a message on another channel than SMS.</summary>
    public ShownSender? Sender { get; init; }

    public string Text { get; init; } = string.Empty;

    /// <summary><c>null</c> for a message on another channel than SMS.</summary>
    public IReadOnlyList<TrackingLink>? Links { get; init; }

    /// <summary><c>null</c> for a message on another channel than SMS.</summary>
    public SmsParts? Parts { get; init; }

    public IReadOnlyList<ReportAddress> ReportTo { get; init; } = [];

    public IReadOnlyList<ReportAddress> ReplyTo { get; init; } = [];

    /// <summary>The request's fields as they arrived, unknown ones and wrong JSON types included.</summary>
    public IReadOnlyDictionary<string, JsonElement> Fields { get; init; } = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

    public IReadOnlyList<MessageWarning> Warnings { get; init; } = [];

    public IReadOnlyList<ReportAttempt> Reports { get; init; } = [];

    /// <summary>The request's body as it arrived.</summary>
    public string Request { get; init; } = string.Empty;
}

/// <summary>The sender as the recipient's phone shows it.</summary>
public sealed record ShownSender
{
    public string Shown { get; init; } = string.Empty;

    public SenderKind Kind { get; init; }

    /// <summary>The request's field the sender came from, <c>replysms</c>, <c>text</c>, <c>number</c> or <c>subject</c>, or <c>account</c>.</summary>
    public string Source { get; init; } = string.Empty;

    public bool FromAccount { get; init; }
}

public sealed record TrackingLink
{
    public TrackingLinkKind Kind { get; init; }

    public string Shown { get; init; } = string.Empty;

    public string? Target { get; init; }
}

public sealed record SmsParts
{
    public SmsCharacterSet CharacterSet { get; init; }

    public int Count { get; init; }

    public int Length { get; init; }

    public IReadOnlyList<int> Starts { get; init; } = [];

    public int LeftInLastPart { get; init; }

    public int MaxParts { get; init; }
}

/// <summary>An address a request names for its delivery reports or its replies.</summary>
public sealed record ReportAddress
{
    public ReportAddressKind Kind { get; init; }

    public string Target { get; init; } = string.Empty;

    public string? ContentType { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

public sealed record MessageWarning
{
    public string Code { get; init; } = string.Empty;

    public string? Field { get; init; }

    public string Text { get; init; } = string.Empty;
}

public sealed record ReportAttempt
{
    public DateTimeOffset At { get; init; }

    public ReportStatus Status { get; init; }

    public string Target { get; init; } = string.Empty;

    public bool Sent { get; init; }

    public string Outcome { get; init; } = string.Empty;
}
