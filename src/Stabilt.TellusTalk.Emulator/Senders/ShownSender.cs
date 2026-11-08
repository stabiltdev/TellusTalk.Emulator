namespace Stabilt.TellusTalk.Emulator.Senders;

/// <summary>The sender as the recipient's phone shows it.</summary>
/// <param name="Display">The name or number the phone shows.</param>
/// <param name="Kind">Whether the phone shows a name or a number.</param>
/// <param name="Source">The source the request asked for: text, number, subject or replysms; account when it asked for none or an undocumented one.</param>
/// <param name="FromAccount">Whether the shown sender is the account's rather than the request's.</param>
internal sealed record ShownSender(string Display, SenderKind Kind, string Source, bool FromAccount);
