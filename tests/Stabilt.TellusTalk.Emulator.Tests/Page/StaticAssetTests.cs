using System.Net;
using AngleSharp.Html.Parser;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Page;

public sealed class StaticAssetTests
{
    [Theory]
    [InlineData("/css/app.css")]
    [InlineData("/js/theme.js")]
    [InlineData("/js/app.js")]
    [InlineData("/favicon.svg")]
    [InlineData("/fonts/schibsted-grotesk-latin-wght-normal.woff2")]
    [InlineData("/fonts/schibsted-grotesk-latin-ext-wght-normal.woff2")]
    public async Task The_page_s_assets_are_served(string path)
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/css/app.css")]
    [InlineData("/js/app.js")]
    [InlineData("/fonts/schibsted-grotesk-latin-wght-normal.woff2")]
    public async Task An_asset_is_checked_with_the_server_before_the_browser_uses_its_copy(string path)
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
    }

    [Fact]
    public async Task The_page_loads_its_own_assets_and_nothing_from_another_origin()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        var html = await client.GetStringAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        using var page = await new HtmlParser().ParseDocumentAsync(html, TestContext.Current.CancellationToken);

        page.QuerySelector("head link[rel='stylesheet']")!.GetAttribute("href").Should().Be("/css/app.css");
        page.QuerySelector("head link[rel='icon']")!.GetAttribute("href").Should().Be("/favicon.svg");
        page.QuerySelectorAll("script[src]")
            .Select(script => script.GetAttribute("src"))
            .Should().Equal("/js/theme.js", "/js/app.js");
        page.QuerySelectorAll("[src], link[href]")
            .Select(element => element.GetAttribute("src") ?? element.GetAttribute("href"))
            .Should().AllSatisfy(address => address.Should().StartWith("/").And.NotStartWith("//"));
    }
}
