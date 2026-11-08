namespace Stabilt.TellusTalk.Emulator;

/// <summary>The emulator's settings, from the section <c>Emulator</c>: in a container, <c>Emulator__RetentionLimit</c> and so on.</summary>
internal sealed class EmulatorOptions
{
    public const string Section = "Emulator";

    /// <summary>How many messages are kept; the oldest go first.</summary>
    public int RetentionLimit { get; set; } = 1000;

    /// <summary>The sender a TellusTalk account is configured with, shown when a request sets none.</summary>
    public string AccountSender { get; set; } = "Emulator";

    /// <summary>How long after a message is sent, or falls due, its delivery report goes out.</summary>
    public int ReportDelaySeconds { get; set; } = 1;

    /// <summary>Numbers whose reports say the operator could not deliver, separated by commas.</summary>
    public string FailingNumbers { get; set; } = "+46701740699";

    public bool Fails(string number) => FailingNumbers
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Contains(number, StringComparer.Ordinal);
}
