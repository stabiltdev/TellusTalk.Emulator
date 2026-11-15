namespace Stabilt.TellusTalk.Emulator.Client;

internal sealed record MessageList
{
    public int Total { get; init; }

    public IReadOnlyList<CaughtMessage> Messages { get; init; } = [];
}
