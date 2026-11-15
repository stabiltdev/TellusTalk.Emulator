using Stabilt.TellusTalk.Emulator.Parts;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>What only an SMS has.</summary>
/// <param name="Sender">The sender as the recipient's phone shows it.</param>
/// <param name="Links">The tracking links the text holds.</param>
/// <param name="MaxParts">The most parts the request allows.</param>
internal sealed record SmsContent(ShownSender Sender, IReadOnlyList<TrackingLink> Links, SmsParts Parts, int MaxParts);
