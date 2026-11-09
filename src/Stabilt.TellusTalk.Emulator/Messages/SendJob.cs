namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>What one send became: the job TellusTalk answers with, and the messages the emulator caught.</summary>
/// <param name="Size">Every recipient of the request, caught or not: TellusTalk's <c>job_size</c>.</param>
internal sealed record SendJob(string JobId, int Size, IReadOnlyList<Message> Messages);
