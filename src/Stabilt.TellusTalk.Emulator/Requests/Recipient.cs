namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>An SMS recipient.</summary>
/// <param name="Number">The address without <c>sms:</c> and without spaces.</param>
/// <param name="AsSent">The address as it arrived.</param>
internal sealed record Recipient(string Number, string AsSent);
