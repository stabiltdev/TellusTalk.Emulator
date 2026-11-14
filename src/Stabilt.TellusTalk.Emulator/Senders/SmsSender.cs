using System.Diagnostics.CodeAnalysis;

namespace Stabilt.TellusTalk.Emulator.Senders;

internal enum SmsSenderKind
{
    ReplySms,
    Text,
    Number,
    Subject,
}

/// <summary>The Sender setting of a TellusTalk user, in the words the TellusTalk portal uses.</summary>
internal sealed record SmsSender(SmsSenderKind Kind, string? Value)
{
    private const string TextPrefix = "text:";
    private const string NumberPrefix = "number:";

    public static bool TryParse(string? text, [NotNullWhen(true)] out SmsSender? sender)
    {
        sender = text is null ? null : Read(text);
        return sender is not null;
    }

    private static SmsSender? Read(string text) => text switch
    {
        "replysms" => new SmsSender(SmsSenderKind.ReplySms, null),
        "subject" => new SmsSender(SmsSenderKind.Subject, null),
        _ when text.StartsWith(TextPrefix, StringComparison.Ordinal) && text.Length > TextPrefix.Length =>
            new SmsSender(SmsSenderKind.Text, text[TextPrefix.Length..]),
        _ when text.StartsWith(NumberPrefix, StringComparison.Ordinal) && text.Length > NumberPrefix.Length =>
            new SmsSender(SmsSenderKind.Number, text[NumberPrefix.Length..]),
        _ => null,
    };
}
