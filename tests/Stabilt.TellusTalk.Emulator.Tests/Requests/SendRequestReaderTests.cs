using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Requests;

public sealed class SendRequestReaderTests
{
    [Fact]
    public void A_body_that_is_not_an_object_is_refused()
    {
        Read("""["sms:+46701740605"]""").Should().Be(new Refused("TellusTalk Emulator: the request body must be a JSON object."));
    }

    [Fact]
    public void A_request_without_to_is_refused()
    {
        Read("""{"text":"Hej"}""").Should().Be(new Refused("TellusTalk Emulator: a request needs 'to' for the emulator to record a message."));
    }

    [Fact]
    public void To_as_a_string_gives_one_recipient()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient("+46701740605", "sms:+46701740605"));
    }

    [Fact]
    public void To_as_a_list_gives_one_recipient_per_address()
    {
        var request = Accepted("""{"to":["sms:+46701740605","sms:+46701740606"],"text":"Hej"}""");

        request.Recipients.Should().Equal(
            new Recipient("+46701740605", "sms:+46701740605"),
            new Recipient("+46701740606", "sms:+46701740606"));
    }

    [Theory]
    [InlineData("email:a@b.se", "email:")]
    [InlineData("fax:+46701740605", "fax:")]
    [InlineData("mms:+46701740605", "mms:")]
    [InlineData("voice:+46701740605", "voice:")]
    [InlineData("group:42", "group:")]
    [InlineData("log:anything", "log:")]
    public void A_recipient_on_another_channel_is_refused_as_the_emulators_limit(string address, string channel)
    {
        var json = JsonSerializer.Serialize(new { to = address, text = "Hej" });

        Read(json).Should().Be(new Refused($"TellusTalk Emulator: the emulator handles sms: recipients only, not '{channel}'. TellusTalk itself accepts them."));
    }

    [Fact]
    public void Recipient_objects_are_refused_as_the_emulators_limit()
    {
        Read("""{"to":[{"address":"sms:+46701740605","login_options":{"method":"bankid"}}],"text":"Hej"}""")
            .Should().Be(new Refused("TellusTalk Emulator: the emulator handles plain sms: recipients only, not recipient objects. TellusTalk itself accepts them."));
    }

    [Fact]
    public void Spaces_in_a_number_are_removed_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46 70 174 06 05","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient("+46701740605", "sms:+46 70 174 06 05"));
        request.Warnings.Should().ContainSingle().Which.Should().Match<Warning>(warning => warning.Code == WarningCodes.SpacesRemoved && warning.Field == "to");
    }

    [Fact]
    public void A_number_without_the_sms_prefix_is_read_as_an_sms_recipient_with_a_warning()
    {
        var request = Accepted("""{"to":"+46701740605","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient("+46701740605", "+46701740605"));
        request.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.MissingPrefix);
    }

    [Fact]
    public void A_number_with_other_characters_than_digits_is_kept_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+4670abc","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient("+4670abc", "sms:+4670abc"));
        request.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.NotDigits);
    }

    [Fact]
    public void A_field_no_schema_defines_is_accepted_with_a_warning_that_names_it()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_orginator_text":"Probe"}""");

        request.Warnings.Should().ContainSingle().Which.Should().Match<Warning>(warning => warning.Code == WarningCodes.UnknownField && warning.Field == "sms_orginator_text");
        request.Fields.Should().ContainKey("sms_orginator_text");
    }

    [Fact]
    public void A_string_where_the_schema_says_boolean_is_kept_as_sent_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_flash":"true"}""");

        request.Warnings.Should().ContainSingle().Which.Should().Match<Warning>(warning => warning.Code == WarningCodes.WrongJsonType && warning.Field == "sms_flash");
        request.Fields["sms_flash"].GetString().Should().Be("true");
    }

    [Fact]
    public void A_string_where_the_schema_says_number_is_read_as_absent_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_max_parts":"3"}""");

        request.MaxParts.Should().BeNull();
        request.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.WrongJsonType);
    }

    [Fact]
    public void A_number_where_the_schema_says_string_is_read_as_its_json_text_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"text","sms_originator_text":123}""");

        request.OriginatorText.Should().Be("123");
        request.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.WrongJsonType);
    }

    [Fact]
    public void A_request_of_known_fields_with_their_schemas_types_gives_no_warning()
    {
        var request = Accepted("""
            {
              "to": ["sms:+46701740605"],
              "text": "Hej",
              "sms_header": "[Test] ",
              "sms_originator_source": "replysms",
              "sms_originator_text": "Probe",
              "sms_max_parts": 3,
              "sms_flash": false,
              "sms_single_thread": true,
              "sms_validity_time": 3600,
              "sms_character_set": "optimize",
              "billing_id": "team1",
              "scheduled_send_time": "2026-11-08T00:10:00",
              "report_to": "https://example.se/reports",
              "reply_to": ["https://example.se/replies"],
              "attachments": [],
              "echo": { "probe": "1" }
            }
            """);

        request.Warnings.Should().BeEmpty();
        request.Should().Match<SendRequest>(read =>
            read.Text == "Hej" &&
            read.Header == "[Test] " &&
            read.OriginatorSource == "replysms" &&
            read.OriginatorText == "Probe" &&
            read.MaxParts == 3 &&
            read.SingleThread &&
            read.CharacterSet == "optimize" &&
            read.ScheduledSendTime == "2026-11-08T00:10:00");
    }

    [Fact]
    public void A_report_address_in_none_of_the_forms_is_left_out_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","report_to":["https://example.se/r","reports"]}""");

        request.ReportTo.Should().ContainSingle().Which.Target.Should().Be("https://example.se/r");
        request.Warnings.Should().ContainSingle().Which.Should().Match<Warning>(warning =>
            warning.Code == WarningCodes.ReportAddressInvalid && warning.Field == "report_to");
    }

    [Fact]
    public void A_reply_address_is_read_like_a_report_address()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","reply_to":"email:ops@example.se"}""");

        request.ReplyTo.Should().ContainSingle().Which.Kind.Should().Be(ReportAddressKind.Email);
    }

    [Fact]
    public void The_deprecated_sms_format_stands_in_for_the_character_set_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_format":"unicode"}""");

        request.CharacterSet.Should().Be("unicode");
        request.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.FormatDeprecated);
    }

    private static ReadResult Read(string json)
    {
        using var document = JsonDocument.Parse(json);

        return SendRequestReader.Read(document.RootElement);
    }

    private static SendRequest Accepted(string json) =>
        Read(json).Should().BeOfType<Accepted>().Subject.Request;
}
