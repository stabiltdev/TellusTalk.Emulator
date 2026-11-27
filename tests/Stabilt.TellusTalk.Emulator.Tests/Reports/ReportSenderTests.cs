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
            emulator.Time.GetUtcNow(),
            ReportStatus.Delivered,
            "https://example.se/reports",
            Sent: true,
            "200 OK",
            FailedReason: null));
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
        attempts.Should().OnlyContain(attempt => attempt.FailedReason == null);
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

    [Theory]
    [InlineData(HttpStatusCode.NoContent, "HTTPError: Error Code: 204\nreason: No Content")]
    [InlineData(HttpStatusCode.Unauthorized, "HTTPError: Error Code: 401\nreason: Unauthorized")]
    [InlineData(HttpStatusCode.InternalServerError, "HTTPError: Error Code: 500\nreason: Internal Server Error")]
    public async Task A_receivers_answer_other_than_200_fails_the_report_with_TellusTalks_reason(
        HttpStatusCode answer,
        string failedReason)
    {
        await using var emulator = new EmulatorFactory();
        emulator.Reports.Answer = answer;
        var message = await Sent(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""");

        var attempts = await Sender(emulator)
            .Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        attempts.Should().ContainSingle().Which.FailedReason.Should().Be(failedReason);
    }

    [Fact]
    public async Task A_receiver_that_does_not_answer_in_time_fails_the_report_with_TellusTalks_reason()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Reports.TimesOut = true;
        var message = await Sent(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""");

        var attempts = await Sender(emulator)
            .Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        var attempt = attempts.Should().ContainSingle().Subject;
        attempt.Sent.Should().BeFalse();
        attempt.Outcome.Should().Be("Not delivered: the receiver did not answer in time.");
        attempt.FailedReason.Should().Be("RequestError: Internal request timed out, url: https://example.se/reports");
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

    [Fact]
    public async Task An_unreachable_receiver_gets_no_failed_reason()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Reports.Unreachable = true;
        var message = await Sent(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""");

        var attempts = await Sender(emulator)
            .Send(message, ReportStatus.Delivered, TestContext.Current.CancellationToken);

        attempts.Should().ContainSingle().Which.FailedReason.Should().BeNull();
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
