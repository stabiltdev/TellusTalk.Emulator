namespace Stabilt.TellusTalk.Emulator.Reports;

/// <summary>One delivery report the emulator tried to send to one report address.</summary>
/// <param name="Sent">Whether the report reached the address's receiver, whatever it answered.</param>
/// <param name="Outcome">The receiver's status, or why the report was not sent.</param>
internal sealed record ReportAttempt(DateTimeOffset At, ReportStatus Status, string Target, bool Sent, string Outcome);
