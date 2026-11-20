using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Sending.Reading;

namespace Stabilt.TellusTalk.Emulator.Tests.Sending.Reading;

public sealed class SendRequestReaderTests
{
    [Fact]
    public void JSON_that_does_not_parse_is_refused()
    {
        Read("{").Should().Be(new Refused("TellusTalk Emulator: the request body is not valid JSON."));
    }

    [Fact]
    public void A_body_that_is_not_an_object_is_refused()
    {
        Read("""["sms:+46701740605"]""").Should().Be(new Refused("TellusTalk Emulator: the request body must be a JSON object."));
    }

    [Theory]
    [InlineData("""{"text":"Hej"}""")]
    [InlineData("""{"to":null,"text":"Hej"}""")]
    [InlineData("""{"to":"","text":"Hej"}""")]
    [InlineData("""{"to":[],"text":"Hej"}""")]
    [InlineData("""{"to":0,"text":"Hej"}""")]
    public void A_to_that_is_missing_or_empty_is_refused_as_k8_does(string json)
    {
        Read(json).Should().Be(new RefusedByTellusTalk("key: to is missing or empty"));
    }

    [Theory]
    [InlineData("""{"to":46701740619,"text":"Hej"}""", "int")]
    [InlineData("""{"to":4.5,"text":"Hej"}""", "float")]
    [InlineData("""{"to":true,"text":"Hej"}""", "bool")]
    public void A_to_of_another_type_is_refused_as_k8_does(string json, string type)
    {
        Read(json).Should().Be(new RefusedByTellusTalk($"key: to is of type <class '{type}'> expected (str,dict,list)"));
    }

    [Theory]
    [InlineData("""{"to":"sms:+46701740605"}""")]
    [InlineData("""{"to":"sms:+46701740605","text":null}""")]
    [InlineData("""{"to":"sms:+46701740605","text":""}""")]
    public void A_text_that_is_missing_or_empty_is_refused_as_k8_does(string json)
    {
        Read(json).Should().Be(new RefusedByTellusTalk("key: text is missing or empty"));
    }

    [Theory]
    [InlineData("""{"to":"sms:+46701740605","text":123}""", "int")]
    [InlineData("""{"to":"sms:+46701740605","text":true}""", "bool")]
    [InlineData("""{"to":"sms:+46701740605","text":["Hej"]}""", "list")]
    public void A_text_of_another_type_is_refused_as_k8_does(string json, string type)
    {
        Read(json).Should().Be(new RefusedByTellusTalk($"key: text is of type {type} expected (str)"));
    }

    [Fact]
    public void A_missing_to_is_refused_before_a_missing_text()
    {
        Read("{}").Should().Be(new RefusedByTellusTalk("key: to is missing or empty"));
    }

    [Fact]
    public void K8s_refusal_of_to_comes_before_the_sender_refusals()
    {
        Read("""{"subject":null,"text":"Hej"}""").Should().Be(new RefusedByTellusTalk("key: to is missing or empty"));
    }

    [Theory]
    [InlineData("null", "'NoneType' object is not iterable")]
    [InlineData("12", "'int' object is not iterable")]
    [InlineData("true", "'bool' object is not iterable")]
    [InlineData("\"file\"", "'str' object has no attribute 'pop'")]
    [InlineData("[\"file\"]", "'str' object has no attribute 'pop'")]
    [InlineData("[{\"name\":\"a.txt\"},3]", "'int' object has no attribute 'pop'")]
    [InlineData("{\"name\":\"a.txt\"}", "'str' object has no attribute 'pop'")]
    public void Attachments_k8_cannot_take_fail_as_k8_does(string attachments, string text)
    {
        Read($$"""{"to":"sms:+46701740605","text":"Hej","attachments":{{attachments}}}""")
            .Should().Be(new FailedInTellusTalk(text));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("\"\"")]
    [InlineData("[{\"name\":\"a.txt\"}]")]
    public void Attachments_k8_takes_are_accepted(string attachments)
    {
        Read($$"""{"to":"sms:+46701740605","text":"Hej","attachments":{{attachments}}}""")
            .Should().BeOfType<Accepted>();
    }

    [Fact]
    public void Attachments_fail_before_k8s_refusals_of_the_other_fields()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","attachments":null,"sms_header":null}""")
            .Should().Be(new FailedInTellusTalk("'NoneType' object is not iterable"));
    }

    [Theory]
    [InlineData("reply_to", "null", "NoneType")]
    [InlineData("reply_to", "123", "int")]
    [InlineData("reply_to", "false", "bool")]
    [InlineData("report_to", "null", "NoneType")]
    [InlineData("report_to", "123", "int")]
    [InlineData("report_to", "1.5", "float")]
    public void An_address_list_of_another_type_is_refused_as_k8_does(string field, string value, string type)
    {
        Read($$"""{"to":"sms:+46701740605","text":"Hej","{{field}}":{{value}}}""")
            .Should().Be(new RefusedByTellusTalk($"400 Bad Request: {field} is of type <class '{type}'>. Expected (list,dict,str)"));
    }

    [Theory]
    [InlineData("sms_validity_time")]
    [InlineData("sms_max_parts")]
    public void A_null_count_is_refused_as_k8_does(string field)
    {
        Read($$"""{"to":"sms:+46701740605","text":"Hej","{{field}}":null}""")
            .Should().Be(new RefusedByTellusTalk($"Invalid {field} not an int: None"));
    }

    [Theory]
    [InlineData("sms_max_parts", "\"2\"")]
    [InlineData("sms_validity_time", "\"60\"")]
    public void A_count_written_as_a_string_is_accepted_with_a_warning(string field, string value)
    {
        var request = Accepted($$"""{"to":"sms:+46701740605","text":"Hej","{{field}}":{{value}}}""");

        request.Warnings.Should().ContainSingle().Which.Should().Match<Warning>(warning =>
            warning.Code == WarningCodes.WrongJsonType && warning.Field == field);
    }

    [Theory]
    [InlineData("null", "None")]
    [InlineData("123", "123")]
    [InlineData("true", "True")]
    public void A_header_that_is_no_string_is_refused_as_k8_does(string value, string shown)
    {
        Read($$"""{"to":"sms:+46701740605","text":"Hej","sms_header":{{value}}}""")
            .Should().Be(new RefusedByTellusTalk($"Invalid sms_header not a string: {shown}"));
    }

    [Fact]
    public void Reply_to_is_refused_before_report_to()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","report_to":123,"reply_to":123}""")
            .Should().Be(new RefusedByTellusTalk("400 Bad Request: reply_to is of type <class 'int'>. Expected (list,dict,str)"));
    }

    [Fact]
    public void The_validity_time_is_refused_before_the_max_parts()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","sms_max_parts":null,"sms_validity_time":null}""")
            .Should().Be(new RefusedByTellusTalk("Invalid sms_validity_time not an int: None"));
    }

    [Fact]
    public void The_max_parts_are_refused_before_the_header()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","sms_header":123,"sms_max_parts":null}""")
            .Should().Be(new RefusedByTellusTalk("Invalid sms_max_parts not an int: None"));
    }

    [Fact]
    public void Report_to_is_refused_before_the_header()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","sms_header":123,"report_to":123}""")
            .Should().Be(new RefusedByTellusTalk("400 Bad Request: report_to is of type <class 'int'>. Expected (list,dict,str)"));
    }

    [Theory]
    [InlineData("""{"sms_character_set":1}""", "Invalid sms_character_set not a string: 1")]
    [InlineData("""{"sms_format":1}""", "Invalid sms_character_set not a string: 1")]
    [InlineData("""{"sms_character_set":"latin1"}""", "Invalid sms_character_set: latin1; must be one of frozenset({'optimize', 'unicode', 'binary', 'gsm'})")]
    [InlineData("""{"sms_character_set":null,"sms_format":"latin1"}""", "Invalid sms_character_set: latin1; must be one of frozenset({'optimize', 'unicode', 'binary', 'gsm'})")]
    [InlineData("""{"sms_character_set":"binary"}""", "400 Bad Request: Binary sms_format missing sms_udh")]
    [InlineData("""{"sms_character_set":"binary","sms_udh":""}""", "400 Bad Request: Binary sms_format missing sms_udh")]
    [InlineData("""{"billing_id":"billing id-1"}""", "Invalid billing_id billing id-1")]
    public void Character_sets_binary_headers_and_billing_ids_k8_refuses_are_refused_with_its_text(string fields, string text)
    {
        Read($$"""{"to":"sms:+46701740605","text":"48656C6C6F",{{fields[1..]}}""")
            .Should().Be(new RefusedByTellusTalk(text));
    }

    [Fact]
    public void The_character_set_decides_over_an_invalid_sms_format()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_character_set":"gsm","sms_format":"latin1"}""");

        request.CharacterSet.Should().Be("gsm");
    }

    [Fact]
    public void An_empty_billing_id_is_accepted()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","billing_id":""}""").Should().BeOfType<Accepted>();
    }

    [Fact]
    public void The_character_set_is_refused_before_the_header()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","sms_header":123,"sms_character_set":1}""")
            .Should().Be(new RefusedByTellusTalk("Invalid sms_character_set not a string: 1"));
    }

    [Theory]
    [InlineData("""{"to":"sms:+46701740615","text":"Probe R-T0: every other field null.","html":null,"echo":null,"scheduled_send_time":null,"billing_id":null,"report_to":null,"reply_to":null,"sms_max_parts":null,"sms_header":null,"sms_flash":null,"sms_validity_time":null,"sms_single_thread":null,"sms_format":null,"sms_character_set":null,"sms_udh":null,"attachments":null}""", "'NoneType' object is not iterable")]
    [InlineData("""{"to":"sms:+46701740618","text":"Probe R-T3: every other field of another type.","html":123,"scheduled_send_time":1763400000,"billing_id":123,"report_to":123,"reply_to":123,"sms_max_parts":"2","sms_header":123,"sms_flash":1,"sms_validity_time":"60","sms_single_thread":"true","sms_format":1,"sms_character_set":1,"sms_udh":123,"attachments":"file"}""", "'str' object has no attribute 'pop'")]
    public void A_request_breaking_several_rules_fails_on_attachments_as_k8_did(string json, string text)
    {
        Read(json).Should().Be(new FailedInTellusTalk(text));
    }

    [Theory]
    [InlineData("""{"to":"sms:+46701740639","text":"Probe R-T0a: every other field null but the lists.","html":null,"echo":null,"scheduled_send_time":null,"billing_id":null,"sms_max_parts":null,"sms_header":null,"sms_flash":null,"sms_validity_time":null,"sms_single_thread":null,"sms_format":null,"sms_character_set":null,"sms_udh":null}""", "Invalid sms_validity_time not an int: None")]
    [InlineData("""{"to":"sms:+46701740643","text":"Probe R-T0e: every other field null but the lists and the validity time.","html":null,"echo":null,"scheduled_send_time":null,"billing_id":null,"sms_max_parts":null,"sms_header":null,"sms_flash":null,"sms_single_thread":null,"sms_format":null,"sms_character_set":null,"sms_udh":null}""", "Invalid sms_max_parts not an int: None")]
    [InlineData("""{"to":"sms:+46701740644","text":"Probe R-T0f: every other field null but the lists, the validity time and the max parts.","html":null,"echo":null,"scheduled_send_time":null,"billing_id":null,"sms_header":null,"sms_flash":null,"sms_single_thread":null,"sms_format":null,"sms_character_set":null,"sms_udh":null}""", "Invalid sms_header not a string: None")]
    [InlineData("""{"to":"sms:+46701740646","text":"Probe R-T3a: every other field of another type but attachments.","html":123,"scheduled_send_time":1763400000,"billing_id":123,"report_to":123,"reply_to":123,"sms_max_parts":"2","sms_header":123,"sms_flash":1,"sms_validity_time":"60","sms_single_thread":"true","sms_format":1,"sms_character_set":1,"sms_udh":123}""", "400 Bad Request: reply_to is of type <class 'int'>. Expected (list,dict,str)")]
    [InlineData("""{"to":"sms:+46701740648","text":"Probe R-T3c: other types, without attachments and reply_to.","html":123,"scheduled_send_time":1763400000,"billing_id":123,"report_to":123,"sms_max_parts":"2","sms_header":123,"sms_flash":1,"sms_validity_time":"60","sms_single_thread":"true","sms_format":1,"sms_character_set":1,"sms_udh":123}""", "400 Bad Request: report_to is of type <class 'int'>. Expected (list,dict,str)")]
    [InlineData("""{"to":"sms:+46701740649","text":"Probe R-T3d: other types, without the lists.","html":123,"scheduled_send_time":1763400000,"billing_id":123,"sms_max_parts":"2","sms_header":123,"sms_flash":1,"sms_validity_time":"60","sms_single_thread":"true","sms_format":1,"sms_character_set":1,"sms_udh":123}""", "Invalid sms_character_set not a string: 1")]
    [InlineData("""{"to":"sms:+46701740650","text":"Probe R-T3e: other types, without the lists and the character set.","html":123,"scheduled_send_time":1763400000,"billing_id":123,"sms_max_parts":"2","sms_header":123,"sms_flash":1,"sms_validity_time":"60","sms_single_thread":"true","sms_format":1,"sms_udh":123}""", "Invalid sms_character_set not a string: 1")]
    [InlineData("""{"to":"sms:+46701740651","text":"Probe R-T3f: other types, without the lists and the character sets.","html":123,"scheduled_send_time":1763400000,"billing_id":123,"sms_max_parts":"2","sms_header":123,"sms_flash":1,"sms_validity_time":"60","sms_single_thread":"true","sms_udh":123}""", "Invalid sms_header not a string: 123")]
    public void A_request_breaking_several_rules_meets_the_refusal_k8_gave_it(string json, string text)
    {
        Read(json).Should().Be(new RefusedByTellusTalk(text));
    }

    [Theory]
    [InlineData("""{"to":"sms:+46701740645","text":"Probe R-T0g: the remaining fields null.","html":null,"echo":null,"scheduled_send_time":null,"billing_id":null,"sms_flash":null,"sms_single_thread":null,"sms_format":null,"sms_character_set":null,"sms_udh":null}""")]
    [InlineData("""{"to":"sms:+46701740652","text":"Probe R-T3g: other types, without the lists, the character sets and the header.","html":123,"scheduled_send_time":1763400000,"billing_id":123,"sms_max_parts":"2","sms_flash":1,"sms_validity_time":"60","sms_single_thread":"true","sms_udh":123}""")]
    public void Fields_k8_took_as_null_or_of_another_type_are_accepted(string json)
    {
        Read(json).Should().BeOfType<Accepted>();
    }

    [Fact]
    public void To_as_a_string_gives_one_recipient()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient(MessageChannel.Sms, "+46701740605", "sms:+46701740605"));
    }

    [Fact]
    public void To_as_a_list_gives_one_recipient_per_address()
    {
        var request = Accepted("""{"to":["sms:+46701740605","sms:+46701740606"],"text":"Hej"}""");

        request.Recipients.Should().Equal(
            new Recipient(MessageChannel.Sms, "+46701740605", "sms:+46701740605"),
            new Recipient(MessageChannel.Sms, "+46701740606", "sms:+46701740606"));
    }

    [Theory]
    [InlineData("sms:+46701740605", nameof(MessageChannel.Sms), "+46701740605")]
    [InlineData("email:test.user@example.se", nameof(MessageChannel.Email), "test.user@example.se")]
    [InlineData("fax:+46701740606", nameof(MessageChannel.Fax), "+46701740606")]
    [InlineData("mms:+46701740606", nameof(MessageChannel.Mms), "+46701740606")]
    [InlineData("voice:+46701740606", nameof(MessageChannel.Voice), "+46701740606")]
    [InlineData("group:box.abc123", nameof(MessageChannel.Group), "box.abc123")]
    public void Each_prefix_gives_its_channel_and_the_address_after_it(string address, string channel, string read)
    {
        var request = Accepted(JsonSerializer.Serialize(new { to = address, text = "Hej" }));

        request.Recipients.Should().Equal(new Recipient(Enum.Parse<MessageChannel>(channel), read, address));
        request.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void A_request_with_recipients_on_other_channels_only_is_accepted()
    {
        var request = Accepted("""{"to":["email:test.user@example.se","fax:+46701740606"],"text":"Hej"}""");

        request.Recipients
            .Select(recipient => recipient.Channel)
            .Should().Equal(MessageChannel.Email, MessageChannel.Fax);
    }

    [Theory]
    [InlineData("fax:+46 70 174 06 06")]
    [InlineData("mms:+46 70 174 06 06")]
    [InlineData("voice:+46 70 174 06 06")]
    public void Spaces_in_a_fax_mms_or_voice_number_are_removed_with_a_warning(string address)
    {
        var request = Accepted(JsonSerializer.Serialize(new { to = address, text = "Hej" }));

        request.Recipients.Single().Address.Should().Be("+46701740606");
        var warning = request.Warnings.Should().ContainSingle().Subject;
        warning.Code.Should().Be(WarningCodes.SpacesRemoved);
        warning.Field.Should().Be("to");
        warning.Text.Should().NotContain("TellusTalk removes");
    }

    [Fact]
    public void A_fax_mms_or_voice_number_of_other_characters_than_digits_is_kept_with_a_warning()
    {
        var request = Accepted("""{"to":"voice:+4670abc","text":"Hej"}""");

        request.Recipients.Single().Address.Should().Be("+4670abc");
        request.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.NotDigits);
    }

    [Theory]
    [InlineData("email:test.user")]
    [InlineData("email:@example.se")]
    [InlineData("email:test.user@")]
    public void An_e_mail_address_without_a_name_at_and_domain_is_kept_with_a_warning(string address)
    {
        var request = Accepted(JsonSerializer.Serialize(new { to = address, text = "Hej" }));

        request.Recipients.Should().Equal(new Recipient(MessageChannel.Email, address["email:".Length..], address));
        request.Warnings.Should().ContainSingle().Which.Should().Match<Warning>(warning =>
            warning.Code == WarningCodes.EmailInvalid && warning.Field == "to");
    }

    [Fact]
    public void A_bare_e_mail_address_is_read_as_an_sms_number_with_warnings()
    {
        var request = Accepted("""{"to":"test.user@example.se","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient(MessageChannel.Sms, "test.user@example.se", "test.user@example.se"));
        request.Warnings
            .Select(warning => warning.Code)
            .Should().Equal(WarningCodes.MissingPrefix, WarningCodes.NotDigits);
    }

    [Fact]
    public void The_sender_and_encoding_rules_are_kept_apart_from_the_requests_warnings()
    {
        var request = Accepted("""{"to":"email:test.user@example.se","subject":"Your order has shipped","text":"Hej","sms_format":"unicode","scheduled_send_time":"tomorrow"}""");

        request.Warnings
            .Select(warning => warning.Code)
            .Should().Equal(WarningCodes.ScheduledTimeInvalid);
        request.SmsWarnings
            .Select(warning => warning.Code)
            .Should().Equal(WarningCodes.SubjectTooLong, WarningCodes.FormatDeprecated);
    }

    [Theory]
    [InlineData("""{"to":"log:job1.sign","text":"Hej"}""")]
    [InlineData("""{"to":["sms:+46701740605","log:job1.sign"],"text":"Hej"}""")]
    public void A_log_recipient_is_refused_as_the_emulators_limit(string json)
    {
        Read(json).Should().Be(new Refused("TellusTalk Emulator: the emulator does not handle log: recipients, which TellusTalk answers with a log_href the emulator cannot give. TellusTalk itself accepts them."));
    }

    [Fact]
    public void Recipient_objects_are_refused_as_the_emulators_limit()
    {
        Read("""{"to":[{"address":"sms:+46701740605","login_options":{"method":"bankid"}}],"text":"Hej"}""")
            .Should().Be(new Refused("TellusTalk Emulator: the emulator handles plain recipients only, not recipient objects. TellusTalk itself accepts them."));
    }

    [Fact]
    public void A_to_that_is_one_recipient_object_is_refused_as_the_emulators_limit()
    {
        Read("""{"to":{"address":"sms:+46701740605"},"text":"Hej"}""")
            .Should().Be(new Refused("TellusTalk Emulator: the emulator handles plain recipients only, not recipient objects. TellusTalk itself accepts them."));
    }

    [Fact]
    public void A_to_list_holding_something_other_than_an_address_is_refused_as_the_emulators_limit()
    {
        Read("""{"to":["sms:+46701740605",46701740606],"text":"Hej"}""")
            .Should().Be(new Refused("TellusTalk Emulator: 'to' must be an address or a list of addresses."));
    }

    [Fact]
    public void Spaces_in_a_number_are_removed_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46 70 174 06 05","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient(MessageChannel.Sms, "+46701740605", "sms:+46 70 174 06 05"));
        request.Warnings.Should().ContainSingle().Which.Should().Match<Warning>(warning => warning.Code == WarningCodes.SpacesRemoved && warning.Field == "to");
    }

    [Fact]
    public void Hyphens_in_a_number_are_removed_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46-70-174-06-21","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient(MessageChannel.Sms, "+46701740621", "sms:+46-70-174-06-21"));
        request.Warnings.Should().Equal(new Warning(
            WarningCodes.HyphensRemoved,
            "to",
            "'sms:+46-70-174-06-21' contains hyphens; TellusTalk removes them, and so does the emulator."));
    }

    [Theory]
    [InlineData("fax:+46-70-174-06-21")]
    [InlineData("mms:+46-70-174-06-21")]
    [InlineData("voice:+46-70-174-06-21")]
    public void Hyphens_in_a_fax_mms_or_voice_number_are_removed_with_a_warning(string address)
    {
        var request = Accepted(JsonSerializer.Serialize(new { to = address, text = "Hej" }));

        request.Recipients.Single().Address.Should().Be("+46701740621");
        request.Warnings.Should().Equal(new Warning(
            WarningCodes.HyphensRemoved,
            "to",
            $"'{address}' contains hyphens; the emulator removes them, as TellusTalk does in an SMS number."));
    }

    [Fact]
    public void A_number_without_the_sms_prefix_is_read_as_an_sms_recipient_with_a_warning()
    {
        var request = Accepted("""{"to":"+46701740605","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient(MessageChannel.Sms, "+46701740605", "+46701740605"));
        request.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.MissingPrefix);
    }

    [Fact]
    public void A_number_with_other_characters_than_digits_is_kept_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+4670abc","text":"Hej"}""");

        request.Recipients.Should().Equal(new Recipient(MessageChannel.Sms, "+4670abc", "sms:+4670abc"));
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

    [Theory]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"subject"}""", "400 Bad Request: Missing/invalid 'subject'. Expected non-empty string, got 'None'.")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"subject","subject":""}""", "400 Bad Request: Missing/invalid 'subject'. Expected non-empty string, got ''.")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"text"}""", "400 Bad Request: Missing/invalid 'sms_originator_text'. Expected string, got 'None'")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"text","sms_originator_text":""}""", "400 Bad Request: Missing/invalid 'sms_originator_text'. Expected string, got ''")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"number"}""", "400 Bad Request: Missing/invalid 'sms_originator_number'. Expected number, got 'None'")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","subject":null}""", "Invalid subject not a string: None")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":null}""", "Invalid sms_originator_source not a string: None")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_text":null}""", "Invalid sms_originator_text not a string: None")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_number":null}""", "Invalid sms_originator_number not a string: None")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","subject":123}""", "Invalid subject not a string: 123")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"nonsense"}""", "Invalid sms_originator_source: nonsense; must be one of frozenset({'replysms', 'number', 'text', 'subject'})")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"text","sms_originator_text":"Acme","subject":null}""", "Invalid subject not a string: None")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"text","sms_originator_text":"Acme","sms_originator_number":46701740699}""", "Invalid sms_originator_number not a string: 46701740699")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"Text"}""", "Invalid sms_originator_source: Text; must be one of frozenset({'replysms', 'number', 'text', 'subject'})")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":""}""", "Invalid sms_originator_source: ; must be one of frozenset({'replysms', 'number', 'text', 'subject'})")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","sms_originator_source":"number","sms_originator_number":""}""", "400 Bad Request: Missing/invalid 'sms_originator_number'. Expected number, got ''")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","subject":true}""", "Invalid subject not a string: True")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","subject":1.50}""", "Invalid subject not a string: 1.5")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","subject":["Acme", 1, null]}""", "Invalid subject not a string: ['Acme', 1, None]")]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej","subject":{"name": "Acme", "on": true}}""", "Invalid subject not a string: {'name': 'Acme', 'on': True}")]
    public void A_sender_rule_TellusTalk_enforces_refuses_the_request_with_its_text(string json, string text)
    {
        Read(json).Should().Be(new RefusedByTellusTalk(text));
    }

    [Fact]
    public void Sender_refusals_come_before_the_emulators_own_limits()
    {
        Read("""{"to":"email:a@example.se","text":"Hej","subject":null}""")
            .Should().Be(new RefusedByTellusTalk("Invalid subject not a string: None"));
    }

    [Fact]
    public void The_type_of_each_sender_field_is_checked_in_order()
    {
        Read("""{"to":"sms:+46701740605","text":"Hej","sms_originator_number":null,"subject":null}""")
            .Should().Be(new RefusedByTellusTalk("Invalid subject not a string: None"));
    }

    [Fact]
    public void Null_in_a_field_that_is_no_sender_field_is_read_as_absent()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_udh":null,"scheduled_send_time":null}""");

        request.Udh.Should().BeNull();
        request.ScheduledSendTime.Should().BeNull();
    }

    [Fact]
    public void A_number_where_the_schema_says_string_is_read_as_its_json_text_with_a_warning()
    {
        var request = Accepted("""{"to":"sms:+46701740605","text":"Hej","sms_udh":123}""");

        request.Udh.Should().Be("123");
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
        request.SmsWarnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.FormatDeprecated);
    }

    private static ReadResult Read(string json) => SendRequestReader.Read(json);

    private static SendRequest Accepted(string json) =>
        Read(json).Should().BeOfType<Accepted>().Subject.Request;
}
