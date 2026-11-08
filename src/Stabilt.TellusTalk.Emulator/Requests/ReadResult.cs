namespace Stabilt.TellusTalk.Emulator.Requests;

internal abstract record ReadResult;

internal sealed record Accepted(SendRequest Request) : ReadResult;

/// <summary>A request the emulator cannot record; the text says it is the emulator's limit, not TellusTalk's.</summary>
internal sealed record Refused(string Text) : ReadResult;
