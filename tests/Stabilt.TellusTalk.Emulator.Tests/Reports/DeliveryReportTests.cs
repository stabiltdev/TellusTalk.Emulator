using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Reports;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Reports;

public sealed class DeliveryReportTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 7, 22, 48, 35, TimeSpan.Zero));

    [Fact]
    public void A_delivered_report_has_TellusTalks_fields_in_TellusTalks_order()
    {
        var message = Message("""{"to":"sms:+46701740605","text":"Probe 1","sms_originator_source":"text","sms_originator_text":"Probe","echo":{"probe":"1-delivered"}}""");

        using var report = Serialize(DeliveryReport.For(message, ReportStatus.Delivered, new DateTimeOffset(2026, 11, 7, 22, 48, 42, 209, TimeSpan.Zero)));

        report.RootElement
            .EnumerateObject()
            .Select(field => field.Name)
            .Should().Equal(
                "log_status", "log_status_code", "failed_reason", "article_id", "to_protocol", "to_address", "to_name",
                "from_address", "from_name", "subject", "text", "html", "echo", "quantity", "log_uuid", "conversation_uuid",
                "job_uuid", "report_time", "failed_details", "text_linebreaksbr", "template_optional_line");

        var root = report.RootElement;
        root.GetProperty("log_status").GetString().Should().Be("txtDELIVERED");
        root.GetProperty("log_status_code").GetInt32().Should().Be(22);
        root.GetProperty("failed_reason").GetString().Should().BeEmpty();
        root.GetProperty("article_id").GetString().Should().Be("SMS_Sent");
        root.GetProperty("to_protocol").GetString().Should().Be("SMS");
        root.GetProperty("to_address").GetString().Should().Be("sms:+46701740605");
        root.GetProperty("to_name").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("from_address").GetString().Should().Be("user_id:tester");
        root.GetProperty("from_name").GetString().Should().BeEmpty();
        root.GetProperty("subject").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("text").GetString().Should().Be("Probe 1");
        root.GetProperty("html").GetString().Should().Be("Probe 1");
        root.GetProperty("echo").GetProperty("probe").GetString().Should().Be("1-delivered");
        root.GetProperty("quantity").GetInt32().Should().Be(1);
        root.GetProperty("log_uuid").GetString().Should().Be(message.Id);
        root.GetProperty("conversation_uuid").GetString().Should().Be(message.Id);
        root.GetProperty("job_uuid").GetString().Should().Be(message.JobId);
        root.GetProperty("report_time").GetString().Should().Be("2026-11-07T22:48:42.209Z");
        root.GetProperty("failed_details").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("text_linebreaksbr").GetString().Should().Be("Probe 1");
        root.GetProperty("template_optional_line").GetString().Should().Be("--------------");
    }

    [Fact]
    public void A_failed_report_says_the_operator_could_not_route_it()
    {
        var message = Message("""{"to":"sms:+46701740699","text":"Probe 2"}""");

        using var report = Serialize(DeliveryReport.For(message, ReportStatus.Failed, _time.GetUtcNow()));

        var root = report.RootElement;
        root.GetProperty("log_status").GetString().Should().Be("txtDELIVERY_FAILED");
        root.GetProperty("log_status_code").GetInt32().Should().Be(21);
        root.GetProperty("failed_reason").GetString().Should().Be("txtFAILED_REASON: txtOPERATOR_ROUTING_ERROR, None");
    }

    [Fact]
    public void Quantity_counts_the_parts()
    {
        var text = "Rad ett\nrad två " + new string('a', 160);
        var message = Message(JsonSerializer.Serialize(new { to = "sms:+46701740605", text }));

        using var report = Serialize(DeliveryReport.For(message, ReportStatus.Delivered, _time.GetUtcNow()));

        report.RootElement.GetProperty("quantity").GetInt32().Should().Be(2);
    }

    [Fact]
    public void A_subject_heads_the_text_with_a_blank_line()
    {
        var message = Message("""{"to":"sms:+46701740605","text":"Your order has shipped.","subject":"Acme"}""");

        using var report = Serialize(DeliveryReport.For(message, ReportStatus.Delivered, _time.GetUtcNow()));

        report.RootElement.GetProperty("text").GetString().Should().Be("Acme\n\nYour order has shipped.");
    }

    [Fact]
    public void An_empty_subject_leaves_the_text_as_the_message()
    {
        var message = Message("""{"to":"sms:+46701740605","text":"Your order has shipped.","subject":""}""");

        using var report = Serialize(DeliveryReport.For(message, ReportStatus.Delivered, _time.GetUtcNow()));

        report.RootElement.GetProperty("text").GetString().Should().Be("Your order has shipped.");
    }

    [Fact]
    public void Html_and_text_linebreaksbr_keep_each_line_break_after_a_br()
    {
        var message = Message("""{"to":"sms:+46701740605","text":"Line one\nline two","subject":"Acme"}""");

        using var report = Serialize(DeliveryReport.For(message, ReportStatus.Delivered, _time.GetUtcNow()));

        report.RootElement.GetProperty("html").GetString().Should().Be("Acme<br>\n<br>\nLine one<br>\nline two");
        report.RootElement.GetProperty("text_linebreaksbr").GetString().Should().Be("Acme<br>\n<br>\nLine one<br>\nline two");
    }

    [Fact]
    public void A_request_without_echo_reports_null()
    {
        var message = Message("""{"to":"sms:+46701740605","text":"Hej"}""");

        using var report = Serialize(DeliveryReport.For(message, ReportStatus.Delivered, _time.GetUtcNow()));

        report.RootElement.GetProperty("echo").ValueKind.Should().Be(JsonValueKind.Null);
    }

    private Message Message(string json)
    {
        using var document = JsonDocument.Parse(json);
        var request = SendRequestReader.Read(document.RootElement).Should().BeOfType<Accepted>().Subject.Request;
        var factory = new MessageFactory(_time, Options.Create(new EmulatorOptions()));

        return factory
            .Create(request, "tester", json)
            .Messages
            .Single();
    }

    private static JsonDocument Serialize(DeliveryReport report) => JsonDocument.Parse(DeliveryReport.Json(report));
}
