namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>Something in a request that TellusTalk's specification forbids, or that TellusTalk quietly corrects.</summary>
internal sealed record Warning(string Code, string? Field, string Text);
