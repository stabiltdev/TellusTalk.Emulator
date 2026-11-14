using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Tests.Senders;

public sealed class SenderSettingsTests
{
    [Fact]
    public void By_default_the_reply_number_is_shown_as_a_fresh_account_does()
    {
        SenderSettings.From(new EmulatorOptions())
            .Should().Be(new SenderSettings(new SmsSender(SmsSenderKind.ReplySms, null), "+46701740698"));
    }

    [Fact]
    public void The_sms_sender_is_read_from_its_setting()
    {
        SenderSettings.From(new EmulatorOptions { SmsSender = "text:Acme" })
            .Should().Be(new SenderSettings(new SmsSender(SmsSenderKind.Text, "Acme"), "+46701740698"));
    }

    [Fact]
    public void An_account_sender_name_stands_for_a_text_sender()
    {
        SenderSettings.From(new EmulatorOptions { AccountSender = "Acme" })
            .Should().Be(new SenderSettings(new SmsSender(SmsSenderKind.Text, "Acme"), "+46701740698"));
    }

    [Fact]
    public void An_account_sender_number_stands_for_replysms_with_that_reply_number()
    {
        SenderSettings.From(new EmulatorOptions { AccountSender = "+46701740697" })
            .Should().Be(new SenderSettings(new SmsSender(SmsSenderKind.ReplySms, null), "+46701740697"));
    }

    [Fact]
    public void A_reply_number_that_is_set_wins_over_an_account_sender_number()
    {
        SenderSettings.From(new EmulatorOptions { AccountSender = "+46701740697", ReplyNumber = "+46701740696" })
            .Should().Be(new SenderSettings(new SmsSender(SmsSenderKind.ReplySms, null), "+46701740696"));
    }

    [Fact]
    public void The_sms_sender_wins_over_the_account_sender()
    {
        SenderSettings.From(new EmulatorOptions { SmsSender = "subject", AccountSender = "Acme" })
            .Should().Be(new SenderSettings(new SmsSender(SmsSenderKind.Subject, null), "+46701740698"));
    }
}
