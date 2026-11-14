using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Tests.Senders;

public sealed class SenderResolverTests
{
    private const string Account = "Emulator";

    private static readonly SendRequest Plain = new()
    {
        Recipients = [new Recipient("+46701740605", "sms:+46701740605")],
        Text = "Hej",
        Body = new SendRequestBody(),
        Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        Warnings = [],
    };

    [Fact]
    public void A_sender_text_is_shown_as_a_name()
    {
        Resolve(Plain with { OriginatorSource = "text", OriginatorText = "Probe" })
            .Should().Be(new ShownSender("Probe", SenderKind.Name, "text", FromAccount: false));
    }

    [Fact]
    public void A_sender_number_is_shown_as_a_number()
    {
        Resolve(Plain with { OriginatorSource = "number", OriginatorNumber = "+46701740699" })
            .Should().Be(new ShownSender("+46701740699", SenderKind.Number, "number", FromAccount: false));
    }

    [Fact]
    public void A_subject_as_sender_is_shown_as_a_name()
    {
        Resolve(Plain with { OriginatorSource = "subject", Subject = "Acme" })
            .Should().Be(new ShownSender("Acme", SenderKind.Name, "subject", FromAccount: false));
    }

    [Fact]
    public void Without_a_source_the_accounts_sender_is_shown()
    {
        Resolve(Plain).Should().Be(new ShownSender(Account, SenderKind.Name, "account", FromAccount: true));
    }

    [Fact]
    public void An_account_sender_of_digits_is_shown_as_a_number()
    {
        SenderResolver.Resolve(Plain, "+46701740698").Kind.Should().Be(SenderKind.Number);
    }

    [Fact]
    public void Replysms_shows_the_accounts_sender_until_TellusTalk_is_seen_doing_otherwise()
    {
        Resolve(Plain with { OriginatorSource = "replysms" })
            .Should().Be(new ShownSender(Account, SenderKind.Name, "replysms", FromAccount: true));
    }

    [Fact]
    public void A_sender_name_over_eleven_characters_is_shown_whole_as_TellusTalk_passes_it_on()
    {
        Resolve(Plain with { OriginatorSource = "text", OriginatorText = "ProbeProbe12" }).Display.Should().Be("ProbeProbe12");
    }

    private static ShownSender Resolve(SendRequest request) => SenderResolver.Resolve(request, Account);
}
