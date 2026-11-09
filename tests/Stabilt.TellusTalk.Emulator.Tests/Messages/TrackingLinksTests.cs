using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Tests.Messages;

public sealed class TrackingLinksTests
{
    [Fact]
    public void Each_redirect_is_shown_as_a_short_link_numbered_from_zero_in_the_order_of_the_text()
    {
        var (text, links) = TrackingLinks.Shorten("Svara här: redirect=https://example.se/quiz?id=123&name=Test eller redirect=http://example.se/b");

        text.Should().Be("Svara här: https://ebox.nu/r_xxxxxx/0 eller https://ebox.nu/r_xxxxxx/1");
        links.Should().Equal(
            new TrackingLink(TrackingLinkKind.Redirect, "https://ebox.nu/r_xxxxxx/0", "https://example.se/quiz?id=123&name=Test"),
            new TrackingLink(TrackingLinkKind.Redirect, "https://ebox.nu/r_xxxxxx/1", "http://example.se/b"));
    }

    [Fact]
    public void The_view_token_is_shown_as_a_link_to_the_messages_page()
    {
        var (text, links) = TrackingLinks.Shorten("Läs hela beskedet: --LINK_VIEW--");

        text.Should().Be("Läs hela beskedet: https://ebox.nu/z_xxxxxx");
        links.Should().Equal(new TrackingLink(TrackingLinkKind.View, "https://ebox.nu/z_xxxxxx", null));
    }

    [Theory]
    [InlineData("Ingen länk här.")]
    [InlineData("redirect=example.se/quiz")]
    [InlineData("--LINK_REPLY--")]
    public void A_text_without_a_redirect_to_a_web_address_or_the_view_token_is_left_as_written(string written)
    {
        var (text, links) = TrackingLinks.Shorten(written);

        text.Should().Be(written);
        links.Should().BeEmpty();
    }
}
