using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Requests;

public sealed class ReportAddressTests
{
    [Fact]
    public void An_email_address_with_its_prefix_is_an_email_report_address()
    {
        var address = ReportAddress.Parse("email:ops@example.se");

        address.Should().NotBeNull();
        address!.Kind.Should().Be(ReportAddressKind.Email);
        address.Target.Should().Be("ops@example.se");
    }

    [Fact]
    public void A_bare_email_address_is_an_email_report_address_as_TellusTalk_treats_it()
    {
        var address = ReportAddress.Parse("ops@example.se");

        address.Should().NotBeNull();
        address!.Kind.Should().Be(ReportAddressKind.Email);
        address.Target.Should().Be("ops@example.se");
    }

    [Fact]
    public void An_sms_number_is_an_sms_report_address()
    {
        var address = ReportAddress.Parse("sms:+46701740605");

        address.Should().NotBeNull();
        address!.Kind.Should().Be(ReportAddressKind.Sms);
        address.Target.Should().Be("+46701740605");
    }

    [Fact]
    public void A_plain_url_is_a_url_report_address_without_settings()
    {
        var address = ReportAddress.Parse("https://example.se/reports");

        address.Should().NotBeNull();
        address!.Kind.Should().Be(ReportAddressKind.Url);
        address.Target.Should().Be("https://example.se/reports");
        address.ContentType.Should().BeNull();
        address.Headers.Should().BeEmpty();
    }

    [Fact]
    public void A_url_with_settings_carries_its_content_type_and_headers()
    {
        var address = ReportAddress.Parse(
            """https:{"content_type":"application/json","headers":{"Authorization":"Basic dGVzdDp0ZXN0"}}://example.se/reports""");

        address.Should().NotBeNull();
        address!.Kind.Should().Be(ReportAddressKind.Url);
        address.Target.Should().Be("https://example.se/reports");
        address.ContentType.Should().Be("application/json");
        address.Headers.Should().Equal(new Dictionary<string, string>(StringComparer.Ordinal) { ["Authorization"] = "Basic dGVzdDp0ZXN0" });
    }

    [Theory]
    [InlineData("https:{not json}://example.se/reports")]
    [InlineData("ftp://example.se")]
    [InlineData("reports")]
    public void A_value_in_none_of_the_forms_is_no_report_address(string value)
    {
        ReportAddress.Parse(value).Should().BeNull();
    }
}
