using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Requests;

internal sealed record SendRequest
{
    public required IReadOnlyList<Recipient> Recipients { get; init; }

    /// <summary>The recipients on other channels than SMS, as sent: TellusTalk sends to them, the emulator does not catch them.</summary>
    public IReadOnlyList<string> NotCaught { get; init; } = [];

    public string? Text { get; init; }

    public string? Subject { get; init; }

    public string? Header { get; init; }

    public string? OriginatorSource { get; init; }

    public string? OriginatorText { get; init; }

    public string? OriginatorNumber { get; init; }

    public string? CharacterSet { get; init; }

    public int? MaxParts { get; init; }

    public string? Udh { get; init; }

    public bool SingleThread { get; init; }

    public string? ScheduledSendTime { get; init; }

    public IReadOnlyList<ReportAddress> ReportTo { get; init; } = [];

    public IReadOnlyList<ReportAddress> ReplyTo { get; init; } = [];

    public required SendRequestBody Body { get; init; }

    /// <summary>Every top-level member as sent and in the order it arrived, unknown ones included: what is shown as sent.</summary>
    public required IReadOnlyDictionary<string, JsonElement> Fields { get; init; }

    public required IReadOnlyList<Warning> Warnings { get; init; }
}
