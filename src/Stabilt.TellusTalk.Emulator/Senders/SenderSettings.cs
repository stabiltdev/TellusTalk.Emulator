using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Senders;

/// <summary>What the TellusTalk user's Sender setting and the account's reply number say, as the emulator is set to show them.</summary>
internal sealed record SenderSettings(SmsSender Sender, string ReplyNumber)
{
    public const string DefaultReplyNumber = "+46701740698";

    public static SenderSettings From(EmulatorOptions options)
    {
        if (SmsSender.TryParse(options.SmsSender, out var configured))
        {
            return new SenderSettings(configured, options.ReplyNumber ?? DefaultReplyNumber);
        }

        if (options.AccountSender is { } account)
        {
            return PhoneNumbers.IsPlusDigits(account)
                ? new SenderSettings(new SmsSender(SmsSenderKind.ReplySms, null), options.ReplyNumber ?? account)
                : new SenderSettings(new SmsSender(SmsSenderKind.Text, account), options.ReplyNumber ?? DefaultReplyNumber);
        }

        return new SenderSettings(new SmsSender(SmsSenderKind.ReplySms, null), options.ReplyNumber ?? DefaultReplyNumber);
    }
}
