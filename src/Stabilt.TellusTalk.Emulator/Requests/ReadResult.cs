namespace Stabilt.TellusTalk.Emulator.Requests;

internal abstract record ReadResult;

internal sealed record Accepted(SendRequest Request) : ReadResult;

/// <summary>A request the emulator cannot record; the text says it is the emulator's limit, not TellusTalk's.</summary>
internal sealed record Refused(string Text) : ReadResult;

/// <summary>A request K8 was seen to refuse; the text is K8's own, word for word.</summary>
internal sealed record RefusedByTellusTalk(string Text) : ReadResult;
