using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Senders;

internal static class SenderResolver
{
    private const string AccountSource = "account";

    /// <summary>
    /// The specification names <c>replysms</c> as the default source, but TellusTalk was seen sending an SMS without a
    /// source from the name the account is configured with. <c>replysms</c> given explicitly is shown the same way
    /// until TellusTalk is seen doing otherwise. A source other than those four, and a source without its value, are
    /// refused before a request gets here.
    /// </summary>
    public static ShownSender Resolve(SendRequest request, string accountSender)
    {
        var source = request.OriginatorSource;
        if (source is null)
        {
            return FromAccount(accountSender, AccountSource);
        }

        var shown = source switch
        {
            "text" => request.OriginatorText,
            "number" => request.OriginatorNumber,
            "subject" => request.Subject,
            _ => null,
        };

        return shown is null
            ? FromAccount(accountSender, source)
            : new ShownSender(shown, KindOf(shown), source, FromAccount: false);
    }

    private static ShownSender FromAccount(string accountSender, string source) =>
        new(accountSender, KindOf(accountSender), source, FromAccount: true);

    private static SenderKind KindOf(string shown) =>
        PhoneNumbers.IsPlusDigits(shown) ? SenderKind.Number : SenderKind.Name;
}
