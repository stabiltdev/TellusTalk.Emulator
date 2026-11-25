using Stabilt.TellusTalk.Emulator.Parts;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>What only an SMS has.</summary>
/// <param name="Sender">The sender as the recipient's phone shows it.</param>
/// <param name="Body">What the phone shows after the header, which a delivery report carries without it.</param>
/// <param name="Parts">How the text as the phone shows it splits into parts.</param>
/// <param name="MaxParts">The most parts the request allows.</param>
internal sealed record SmsContent(ShownSender Sender, string Body, SmsParts Parts, int MaxParts);
