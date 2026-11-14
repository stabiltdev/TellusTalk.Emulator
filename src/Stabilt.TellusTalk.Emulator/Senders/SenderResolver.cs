using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Senders;

internal static class SenderResolver
{
    private const string AccountSource = "account";
    private const string ReplySmsSource = "replysms";

    /// <summary>
    /// A request that names no source shows what the user's Sender setting says, and <c>replysms</c> shows the account's
    /// reply number whatever that setting is. <c>text</c>, <c>number</c> and <c>subject</c> show the field they name,
    /// which a request that gets here always holds.
    /// </summary>
    public static ShownSender Resolve(SendRequest request, SenderSettings settings)
    {
        var source = request.OriginatorSource;
        return source switch
        {
            null => FromSetting(settings),
            ReplySmsSource => ReplyNumber(settings, ReplySmsSource),
            "text" => Own(request.OriginatorText!, source),
            "number" => Own(request.OriginatorNumber!, source),
            _ => Own(request.Subject!, source),
        };
    }

    private static ShownSender FromSetting(SenderSettings settings) => settings.Sender switch
    {
        { Kind: SmsSenderKind.Text, Value: { } name } => new ShownSender(name, SenderKind.Name, AccountSource, FromAccount: true),
        { Kind: SmsSenderKind.Number, Value: { } number } => new ShownSender(number, SenderKind.Number, AccountSource, FromAccount: true),
        _ => ReplyNumber(settings, AccountSource),
    };

    private static ShownSender ReplyNumber(SenderSettings settings, string source) =>
        new(settings.ReplyNumber, SenderKind.Number, source, FromAccount: true);

    private static ShownSender Own(string shown, string source) =>
        new(shown, PhoneNumbers.IsPlusDigits(shown) ? SenderKind.Number : SenderKind.Name, source, FromAccount: false);
}
