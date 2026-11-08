using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Senders;

internal static class SenderResolver
{
    private const string AccountSource = "account";

    /// <summary>
    /// The specification names <c>replysms</c> as the default source, but TellusTalk was seen sending an SMS without a
    /// source from the name the account is configured with. <c>replysms</c> given explicitly is shown the same way
    /// until TellusTalk is seen doing otherwise.
    /// </summary>
    public static ShownSender Resolve(SendRequest request, string accountSender, ICollection<Warning> warnings)
    {
        var source = request.OriginatorSource;
        if (source is null || !SpecificationRules.OriginatorSources.Contains(source))
        {
            return FromAccount(accountSender, AccountSource);
        }

        var (shown, field) = source switch
        {
            "text" => (request.OriginatorText, "sms_originator_text"),
            "number" => (request.OriginatorNumber, "sms_originator_number"),
            "subject" => (request.Subject, "subject"),
            _ => (null, null),
        };

        if (shown is not null)
        {
            return new ShownSender(shown, KindOf(shown), source, FromAccount: false);
        }

        if (field is not null)
        {
            warnings.Add(new Warning(
                WarningCodes.OriginatorMissing,
                field,
                $"The sender source is {source} but {field} is missing; the emulator shows the account's sender."));
        }

        return FromAccount(accountSender, source);
    }

    private static ShownSender FromAccount(string accountSender, string source) =>
        new(accountSender, KindOf(accountSender), source, FromAccount: true);

    private static SenderKind KindOf(string shown) =>
        PhoneNumbers.IsPlusDigits(shown) ? SenderKind.Number : SenderKind.Name;
}
