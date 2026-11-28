namespace Stabilt.TellusTalk.Emulator.Reports;

/// <summary>One delivery report the emulator tried to send to one report address.</summary>
/// <param name="Sent">Whether the report reached the address's receiver, whatever it answered.</param>
/// <param name="Outcome">The receiver's status, or why the report was not sent.</param>
/// <param name="FailedReason">
/// The <c>failed_reason</c> TellusTalk's log gives a report it fails, where its text has been seen; <c>null</c>
/// otherwise.
/// </param>
internal sealed record ReportAttempt(
    DateTimeOffset At,
    ReportStatus Status,
    string Target,
    bool Sent,
    string Outcome,
    string? FailedReason);
