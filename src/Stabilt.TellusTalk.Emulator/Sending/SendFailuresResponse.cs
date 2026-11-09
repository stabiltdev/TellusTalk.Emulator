namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>
/// The armed send failures as the read API serves them; <c>status</c> and <c>delaySeconds</c> are <c>null</c> when none
/// is armed.
/// </summary>
internal sealed record SendFailuresResponse(int? Status, int Remaining, int? DelaySeconds)
{
    public static SendFailuresResponse From(ArmedFailures? armed) => new(armed?.Status, armed?.Remaining ?? 0, armed?.DelaySeconds);
}
