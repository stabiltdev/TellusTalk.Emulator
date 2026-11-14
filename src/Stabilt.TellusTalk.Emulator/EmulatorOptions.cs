namespace Stabilt.TellusTalk.Emulator;

/// <summary>The emulator's settings, from the section <c>Emulator</c>: in a container, <c>Emulator__RetentionLimit</c> and so on.</summary>
internal sealed class EmulatorOptions
{
    public const string Section = "Emulator";

    /// <summary>How many messages are kept; the oldest go first.</summary>
    public int RetentionLimit { get; set; } = 1000;

    /// <summary>
    /// The user's Sender setting in TellusTalk, shown when a request names no source: <c>replysms</c>, <c>text:&lt;name&gt;</c>,
    /// <c>number:&lt;number&gt;</c> or <c>subject</c>. Without it the setting is <c>replysms</c>.
    /// </summary>
    public string? SmsSender { get; set; }

    /// <summary>The number TellusTalk assigns the account for replies, shown by <c>replysms</c>. Without it the number is <c>+46701740698</c>.</summary>
    public string? ReplyNumber { get; set; }

    /// <summary>Obsolete: use <see cref="SmsSender"/> and <see cref="ReplyNumber"/>. Honoured only when <see cref="SmsSender"/> is not set.</summary>
    public string? AccountSender { get; set; }

    /// <summary>How long after a message is sent, or falls due, its delivery report goes out.</summary>
    public int ReportDelaySeconds { get; set; } = 1;

    /// <summary>Numbers whose reports say the operator could not deliver, separated by commas.</summary>
    public string FailingNumbers { get; set; } = string.Empty;

    public bool Fails(string number) => FailingNumbers
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Contains(number, StringComparer.Ordinal);
}
