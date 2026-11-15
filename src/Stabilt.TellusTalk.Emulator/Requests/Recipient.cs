namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>A recipient on one channel.</summary>
/// <param name="Channel">The channel the address names.</param>
/// <param name="Address">The address without its prefix; for a number channel, without spaces.</param>
/// <param name="AsSent">The address as it arrived.</param>
internal sealed record Recipient(MessageChannel Channel, string Address, string AsSent);
