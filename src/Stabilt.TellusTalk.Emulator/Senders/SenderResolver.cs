using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Senders;

internal static class SenderResolver
{
    private const string AccountSource = "account";
    private const string ReplySmsSource = "replysms";
    private const string SubjectSource = "subject";
    private const string TextSource = "text";
    private const string NumberSource = "number";

    /// <summary>
    /// The source the request decides: the one it names, else the first of <c>subject</c>, <c>sms_originator_text</c> and
    /// <c>sms_originator_number</c> it holds, empty or not. Null when it holds none, so the user's Sender setting decides.
    /// </summary>
    public static string? RequestSource(SendRequest request)
    {
        if (request.OriginatorSource is { } named)
        {
            return named;
        }

        if (request.Subject is not null)
        {
            return SubjectSource;
        }

        if (request.OriginatorText is not null)
        {
            return TextSource;
        }

        return request.OriginatorNumber is not null ? NumberSource : null;
    }

    /// <summary>
    /// <c>replysms</c> shows the account's reply number whatever the user's Sender setting says. <c>text</c>, <c>number</c>
    /// and <c>subject</c> show their field, which a request that gets here holds as a string, and non-empty when it named
    /// the source; an empty one that TellusTalk chose shows the reply number instead.
    /// </summary>
    public static ShownSender Resolve(SendRequest request, SenderSettings settings)
    {
        var source = RequestSource(request);
        return source switch
        {
            null => FromSetting(settings),
            ReplySmsSource => ReplyNumber(settings, ReplySmsSource),
            SubjectSource => FromField(request.Subject!, source, settings),
            TextSource => FromField(request.OriginatorText!, source, settings),
            NumberSource => FromField(request.OriginatorNumber!, source, settings),
            _ => throw new InvalidOperationException($"The reader refuses the sender source '{source}'."),
        };
    }

    private static ShownSender FromSetting(SenderSettings settings) => settings.Sender switch
    {
        { Kind: SmsSenderKind.Text or SmsSenderKind.Number, Value: { } shown } => Shown(shown, AccountSource, fromAccount: true),
        { Kind: SmsSenderKind.ReplySms or SmsSenderKind.Subject } => ReplyNumber(settings, AccountSource),
        var other => throw new InvalidOperationException($"The Sender setting '{other.Kind}' has no way to be shown."),
    };

    private static ShownSender FromField(string shown, string source, SenderSettings settings) =>
        shown.Length == 0 ? ReplyNumber(settings, source) : Shown(shown, source, fromAccount: false);

    private static ShownSender ReplyNumber(SenderSettings settings, string source) =>
        Shown(settings.ReplyNumber, source, fromAccount: true);

    private static ShownSender Shown(string shown, string source, bool fromAccount) =>
        new(shown, PhoneNumbers.IsPlusDigits(shown) ? SenderKind.Number : SenderKind.Name, source, fromAccount);
}
