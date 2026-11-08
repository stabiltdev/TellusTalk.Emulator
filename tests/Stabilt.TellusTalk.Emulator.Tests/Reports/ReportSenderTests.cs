using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Reports;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Reports;

public sealed class ReportSenderTests
{
    private const string Uuid1 = "^[0-9a-f]{12}1[0-9a-f]{3}[89ab][0-9a-f]{15}$";

    [Fact]
    public async Task A_report_is_posted_as_JSON_to_a_url_with_TellusTalks_headers()
    {
        await using var emulator = new EmulatorFactory();
        var message = await Sent(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""");

        var attempts = await Sender(emulator).Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        var received = emulator.Reports.Received.Should().ContainSingle().Subject;
        received.Method.Should().Be(HttpMethod.Post);
        received.Uri.Should().Be(new Uri("https://example.se/reports"));
        received.Headers["Content-Type"].Should().Be("application/json");
        received.Headers["Accept"].Should().Be("*/*");
        received.Headers["webpages-request-id"].Should().MatchRegex(Uuid1);
        received.Headers["x-webpages-request-id"].Should().Be(received.Headers["webpages-request-id"]);
        received.Headers["User-Agent"].Should().Contain("TellusTalk.Emulator");
        using var body = JsonDocument.Parse(received.Body);
        body.RootElement.GetProperty("log_status").GetString().Should().Be("txtDELIVERED");

        attempts.Should().ContainSingle().Which.Should().Be(new ReportAttempt(
            emulator.Time.GetUtcNow(), ReportStatus.Delivered, "https://example.se/reports", Sent: true, "200 OK"));
    }

    [Fact]
    public async Task A_header_in_the_addresss_settings_is_sent_as_given()
    {
        await using var emulator = new EmulatorFactory();
        var message = await Sent(emulator, JsonSerializer.Serialize(new
        {
            to = "sms:+46701740605",
            text = "Hej",
            report_to = """https:{"headers":{"Authorization":"Basic dGVzdDp0ZXN0"}}://example.se/reports""",
        }));

        await Sender(emulator).Send(message, ReportStatus.Failed, TestContext.Current.CancellationToken);

        emulator.Reports.Received.Should().ContainSingle().Which.Headers["Authorization"].Should().Be("Basic dGVzdDp0ZXN0");
    }

    [Fact]
    public async Task E_mail_and_SMS_report_addresses_are_not_sent_to()
    {
        await using var emulator = new EmulatorFactory();
        var message = await Sent(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":["email:ops@example.se","sms:+46701740698"]}""");

        var attempts = await Sender(emulator).Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        emulator.Reports.Received.Should().BeEmpty();
        attempts.Should().SatisfyRespectively(
            email => email.Should().Match<ReportAttempt>(attempt => !attempt.Sent && attempt.Outcome.Contains("e-mail")),
            sms => sms.Should().Match<ReportAttempt>(attempt => !attempt.Sent && attempt.Outcome.Contains("SMS")));
    }

    [Fact]
    public async Task A_report_format_other_than_JSON_is_not_sent()
    {
        await using var emulator = new EmulatorFactory();
        var message = await Sent(emulator, JsonSerializer.Serialize(new
        {
            to = "sms:+46701740605",
            text = "Hej",
            report_to = """https:{"content_type":"application/xml"}://example.se/reports""",
        }));

        var attempts = await Sender(emulator).Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        emulator.Reports.Received.Should().BeEmpty();
        attempts.Should().ContainSingle().Which.Outcome.Should().Contain("JSON");
    }

    [Fact]
    public async Task The_receivers_answer_is_recorded()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Reports.Answer = HttpStatusCode.InternalServerError;
        var message = await Sent(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""");

        var attempts = await Sender(emulator).Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        attempts.Should().ContainSingle().Which.Should().Match<ReportAttempt>(attempt => attempt.Sent && attempt.Outcome == "500 Internal Server Error");
    }

    [Fact]
    public async Task An_unreachable_receiver_is_recorded_as_not_delivered()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Reports.Unreachable = true;
        var message = await Sent(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""");

        var attempts = await Sender(emulator).Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        attempts.Should().ContainSingle().Which.Should().Match<ReportAttempt>(attempt => !attempt.Sent && attempt.Outcome.StartsWith("Not delivered:", StringComparison.Ordinal));
    }

    private static ReportSender Sender(EmulatorFactory emulator) => emulator.Services.GetRequiredService<ReportSender>();

    private static async Task<Message> Sent(EmulatorFactory emulator, string json)
    {
        using var response = await emulator.Send(json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return emulator.Services
            .GetRequiredService<MessageStore>()
            .List()
            .Single();
    }
}
