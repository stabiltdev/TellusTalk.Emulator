using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Requests;

public sealed class SpecificationRulesTests
{
    private static readonly SendRequest Plain = new()
    {
        Recipients = [new Recipient("+46701740605", "sms:+46701740605")],
        Text = "Hej",
        Body = new SendRequestBody(),
        Fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal),
        Warnings = [],
    };

    [Fact]
    public void A_sender_name_over_eleven_characters_is_warned_about()
    {
        SpecificationRules.Check(Plain with { OriginatorSource = "text", OriginatorText = "ProbeProbe12" })
            .Should().ContainSingle().Which.Should().Match<Warning>(warning =>
                warning.Code == WarningCodes.OriginatorTextTooLong && warning.Field == "sms_originator_text");
    }

    [Fact]
    public void A_sender_name_of_eleven_characters_is_not_warned_about()
    {
        SpecificationRules.Check(Plain with { OriginatorSource = "text", OriginatorText = "ProbeProbe1" }).Should().BeEmpty();
    }

    [Fact]
    public void A_subject_as_sender_over_eleven_characters_is_warned_about()
    {
        SpecificationRules.Check(Plain with { OriginatorSource = "subject", Subject = "AcmeMessages" })
            .Should().ContainSingle().Which.Should().Match<Warning>(warning =>
                warning.Code == WarningCodes.SubjectTooLong && warning.Field == "subject");
    }

    [Fact]
    public void A_subject_over_eleven_characters_chosen_without_a_source_is_warned_about()
    {
        SpecificationRules.Check(Plain with { Subject = "AcmeMessages" })
            .Should().ContainSingle().Which.Should().Match<Warning>(warning =>
                warning.Code == WarningCodes.SubjectTooLong && warning.Field == "subject");
    }

    [Fact]
    public void A_long_subject_beside_a_sender_text_chosen_without_a_source_is_not_warned_about_as_the_sender()
    {
        SpecificationRules.Check(Plain with { OriginatorSource = "text", OriginatorText = "Probe", Subject = "AcmeMessages" })
            .Should().BeEmpty();
    }

    [Fact]
    public void A_sender_number_that_is_not_plus_and_digits_is_warned_about()
    {
        SpecificationRules.Check(Plain with { OriginatorSource = "number", OriginatorNumber = "0701740605" })
            .Should().ContainSingle().Which.Code.Should().Be(WarningCodes.OriginatorNumberInvalid);
    }

    [Fact]
    public void An_undocumented_character_set_is_warned_about()
    {
        SpecificationRules.Check(Plain with { CharacterSet = "latin1" })
            .Should().ContainSingle().Which.Code.Should().Be(WarningCodes.CharacterSetInvalid);
    }

    [Fact]
    public void The_deprecated_sms_format_is_warned_about()
    {
        SpecificationRules.Check(Plain with { CharacterSet = "gsm", Body = BodyOf("""{"sms_format":"gsm"}""") })
            .Should().ContainSingle().Which.Should().Match<Warning>(warning =>
                warning.Code == WarningCodes.FormatDeprecated && warning.Field == "sms_format");
    }

    [Fact]
    public void Binary_without_a_user_data_header_is_warned_about()
    {
        SpecificationRules.Check(Plain with { CharacterSet = "binary", Text = "48656C6C6F" })
            .Should().ContainSingle().Which.Code.Should().Be(WarningCodes.BinaryWithoutUdh);
    }

    [Fact]
    public void Binary_with_a_text_that_is_not_hex_is_warned_about()
    {
        SpecificationRules.Check(Plain with { CharacterSet = "binary", Udh = "050003CC0201", Text = "0x41" })
            .Should().ContainSingle().Which.Should().Match<Warning>(warning =>
                warning.Code == WarningCodes.BinaryNotHex && warning.Field == "text");
    }

    [Fact]
    public void Max_parts_below_one_is_warned_about()
    {
        SpecificationRules.Check(Plain with { MaxParts = 0 })
            .Should().ContainSingle().Which.Code.Should().Be(WarningCodes.MaxPartsInvalid);
    }

    [Fact]
    public void A_billing_id_that_is_not_alphanumeric_is_warned_about()
    {
        SpecificationRules.Check(Plain with { Body = BodyOf("""{"billing_id":"team-1"}""") })
            .Should().ContainSingle().Which.Should().Match<Warning>(warning =>
                warning.Code == WarningCodes.BillingIdInvalid && warning.Field == "billing_id");
    }

    [Fact]
    public void A_scheduled_time_that_is_no_ISO_8601_time_is_warned_about()
    {
        SpecificationRules.Check(Plain with { ScheduledSendTime = "tomorrow" })
            .Should().ContainSingle().Which.Code.Should().Be(WarningCodes.ScheduledTimeInvalid);
    }

    private static SendRequestBody BodyOf(string json) => JsonSerializer.Deserialize<SendRequestBody>(json)!;
}
