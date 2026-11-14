using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator.Tests.Senders;

public sealed class SmsSenderTests
{
    [Theory]
    [InlineData("replysms", nameof(SmsSenderKind.ReplySms), null)]
    [InlineData("subject", nameof(SmsSenderKind.Subject), null)]
    [InlineData("text:Acme", nameof(SmsSenderKind.Text), "Acme")]
    [InlineData("number:+46701740697", nameof(SmsSenderKind.Number), "+46701740697")]
    public void A_setting_in_TellusTalks_words_is_read(string text, string kind, string? value)
    {
        SmsSender.TryParse(text, out var sender).Should().BeTrue();
        sender.Should().Be(new SmsSender(Enum.Parse<SmsSenderKind>(kind), value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ReplySms")]
    [InlineData("text:")]
    [InlineData("number:")]
    [InlineData("name:Acme")]
    [InlineData("text")]
    public void Anything_else_is_not_a_setting(string text)
    {
        SmsSender.TryParse(text, out _).Should().BeFalse();
    }
}
