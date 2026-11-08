namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed record SendJob(string JobId, IReadOnlyList<Message> Messages);
