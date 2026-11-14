using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Tests.Senders;

public sealed class SenderResolverTests
{
    private const string ReplyNumber = SenderSettings.DefaultReplyNumber;

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
    public void Without_sender_fields_a_text_sender_shows_its_name()
    {
        SenderResolver.Resolve(Plain, Settings(SmsSenderKind.Text, "Acme"))
            .Should().Be(new ShownSender("Acme", SenderKind.Name, "account", FromAccount: true));
    }

    [Fact]
    public void Without_sender_fields_a_number_sender_shows_its_number()
    {
        SenderResolver.Resolve(Plain, Settings(SmsSenderKind.Number, "+46701740697"))
            .Should().Be(new ShownSender("+46701740697", SenderKind.Number, "account", FromAccount: true));
    }

    [Theory]
    [InlineData(nameof(SmsSenderKind.ReplySms))]
    [InlineData(nameof(SmsSenderKind.Subject))]
    public void Without_sender_fields_replysms_and_subject_senders_show_the_reply_number(string kind)
    {
        SenderResolver.Resolve(Plain, Settings(Enum.Parse<SmsSenderKind>(kind), null))
            .Should().Be(new ShownSender(ReplyNumber, SenderKind.Number, "account", FromAccount: true));
    }

    [Fact]
    public void Replysms_shows_the_reply_number_whatever_the_sms_sender()
    {
        SenderResolver.Resolve(Plain with { OriginatorSource = "replysms" }, Settings(SmsSenderKind.Text, "Acme"))
            .Should().Be(new ShownSender(ReplyNumber, SenderKind.Number, "replysms", FromAccount: true));
    }

    [Fact]
    public void A_sender_name_over_eleven_characters_is_shown_whole_as_TellusTalk_passes_it_on()
    {
        Resolve(Plain with { OriginatorSource = "text", OriginatorText = "ProbeProbe12" }).Display.Should().Be("ProbeProbe12");
    }

    private static ShownSender Resolve(SendRequest request) => SenderResolver.Resolve(request, Settings(SmsSenderKind.ReplySms, null));

    private static SenderSettings Settings(SmsSenderKind kind, string? value) => new(new SmsSender(kind, value), ReplyNumber);
}
