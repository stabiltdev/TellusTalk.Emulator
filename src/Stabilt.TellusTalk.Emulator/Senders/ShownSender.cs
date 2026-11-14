namespace Stabilt.TellusTalk.Emulator.Senders;

/// <summary>The sender as the recipient's phone shows it.</summary>
/// <param name="Display">The name or number the phone shows.</param>
/// <param name="Kind">Whether the phone shows a name or a number.</param>
/// <param name="Source">The source that decided: the one the request named, else the field TellusTalk chose (subject, text or number); account when the user's Sender setting decided.</param>
/// <param name="FromAccount">Whether the shown sender came from the settings, the user's Sender or the reply number, rather than from the request.</param>
internal sealed record ShownSender(string Display, SenderKind Kind, string Source, bool FromAccount);
