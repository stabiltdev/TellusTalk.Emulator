using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Messages;

public sealed class MessageFactoryTests
{
    private const string Welcome =
        "Välkommen till kliniken! Vi har tagit emot din intresseanmälan och en av våra koordinatorer ringer dig inom två " +
        "arbetsdagar. Logga in med e-post för att följa ärendet och se vilka dokument vi behöver från dig.";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 7, 22, 0, 0, TimeSpan.Zero));

    [Fact]
    public void A_single_recipients_message_takes_the_jobs_id()
    {
        var job = Create("""{"to":"sms:+46701740605","text":"Hej"}""");

        job.Messages.Should().ContainSingle().Which.Id.Should().Be(job.JobId);
    }

    [Fact]
    public void Each_of_several_recipients_gets_a_message_of_its_own_in_the_same_job()
    {
        var job = Create("""{"to":["sms:+46701740605","sms:+46701740606"],"text":"Hej"}""");

        job.Messages.Should().HaveCount(2);
        job.Messages.Should().AllSatisfy(message => message.JobId.Should().Be(job.JobId));
        job.Messages
            .Select(message => message.Id)
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void The_header_goes_in_front_of_the_text_and_the_parts_are_counted_on_both()
    {
        var message = Create("""{"to":"sms:+46701740605","text":"Hej","sms_header":"[Test] "}""").Messages.Single();

        message.Text.Should().Be("[Test] Hej");
        message.Parts.Length.Should().Be(12, "the brackets come from the GSM extension table and take two septets each");
    }

    [Fact]
    public void A_character_outside_the_GSM_alphabet_under_forced_GSM_is_warned_about()
    {
        var message = Create("""{"to":"sms:+46701740605","text":"Tack 😊","sms_character_set":"gsm"}""").Messages.Single();

        message.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.GsmUnsupportedCharacters);
    }

    [Fact]
    public void A_text_needing_more_parts_than_allowed_is_kept_whole_with_a_warning()
    {
        var message = Create(Json(new { to = "sms:+46701740605", text = Welcome, sms_max_parts = 1 })).Messages.Single();

        message.Text.Should().Be(Welcome);
        message.Warnings.Should().ContainSingle().Which.Code.Should().Be(WarningCodes.MaxPartsExceeded);
    }

    [Fact]
    public void Without_max_parts_the_default_of_eight_applies()
    {
        var message = Create(Json(new { to = "sms:+46701740605", text = Welcome })).Messages.Single();

        message.MaxParts.Should().Be(8);
        message.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void A_message_scheduled_for_later_waits()
    {
        var message = Create("""{"to":"sms:+46701740605","text":"Hej","scheduled_send_time":"2026-11-07T23:00:00Z"}""").Messages.Single();

        message.DueAt.Should().Be(new DateTimeOffset(2026, 11, 7, 23, 0, 0, TimeSpan.Zero));
        message.StateAt(_time.GetUtcNow()).Should().Be(MessageState.Scheduled);
    }

    [Fact]
    public void A_message_scheduled_in_the_past_is_sent_at_once()
    {
        var message = Create("""{"to":"sms:+46701740605","text":"Hej","scheduled_send_time":"2026-11-07T21:00:00Z"}""").Messages.Single();

        message.DueAt.Should().BeNull();
        message.StateAt(_time.GetUtcNow()).Should().Be(MessageState.Sent);
    }

    [Fact]
    public void A_message_keeps_the_user_the_request_and_every_warning()
    {
        const string body = """{"to":"sms:+46 70 174 06 05","text":"Hej","sms_originator_source":"text"}""";

        var message = Create(body).Messages.Single();

        message.UserName.Should().Be("tester");
        message.RawRequest.Should().Be(body);
        message.Warnings
            .Select(warning => warning.Code)
            .Should().BeEquivalentTo([WarningCodes.SpacesRemoved, WarningCodes.OriginatorMissing]);
    }

    private SendJob Create(string json)
    {
        using var document = JsonDocument.Parse(json);
        var request = SendRequestReader.Read(document.RootElement).Should().BeOfType<Accepted>().Subject.Request;
        var factory = new MessageFactory(_time, Options.Create(new EmulatorOptions()));

        return factory.Create(request, "tester", json);
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
