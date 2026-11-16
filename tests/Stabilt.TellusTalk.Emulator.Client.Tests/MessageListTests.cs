using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Client.Tests;

public sealed class MessageListTests
{
    [Fact]
    public async Task Messages_are_listed_newest_first()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740606","text":"Först"}""");
        emulator.Time.Advance(TimeSpan.FromMinutes(1));
        await Send(emulator, """{"to":"sms:+46701740605","text":"Sedan"}""");

        var messages = await client.GetMessagesAsync(cancellationToken: TestContext.Current.CancellationToken);

        messages
            .Select(message => message.Text)
            .Should().Equal("Sedan", "Först");
    }

    [Fact]
    public async Task An_emulator_without_messages_lists_none()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());

        var messages = await client.GetMessagesAsync(cancellationToken: TestContext.Current.CancellationToken);

        messages.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task The_state_filter_lists_only_that_state()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var cancellation = TestContext.Current.CancellationToken;
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Senare","scheduled_send_time":"2026-11-08T10:00:00Z"}""");

        var sent = await client.GetMessagesAsync(new MessageQuery { State = MessageState.Sent }, cancellation);
        var scheduled = await client.GetMessagesAsync(new MessageQuery { State = MessageState.Scheduled }, cancellation);
        var blocked = await client.GetMessagesAsync(new MessageQuery { State = MessageState.Blocked }, cancellation);
        var all = await client.GetMessagesAsync(cancellationToken: cancellation);

        sent.Should().ContainSingle().Which.State.Should().Be(MessageState.Sent);
        scheduled.Should().ContainSingle().Which.State.Should().Be(MessageState.Scheduled);
        blocked.Should().ContainSingle().Which.State.Should().Be(MessageState.Blocked);
        all.Should().HaveCount(3);
    }

    [Fact]
    public async Task The_channel_filter_lists_only_that_channel()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740605","text":"Ett sms"}""");
        await Send(emulator, """{"to":"email:test.user@example.se","subject":"Hello","text":"Ett mejl"}""");
        await Send(emulator, """{"to":"mms:+46701740605","text":"Ett mms"}""");

        var messages = await client.GetMessagesAsync(new MessageQuery { Channel = MessageChannel.Email }, TestContext.Current.CancellationToken);

        messages.Should().ContainSingle().Which.Channel.Should().Be(MessageChannel.Email);
    }

    [Fact]
    public async Task A_number_with_its_plus_finds_its_messages()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740605","text":"Till den första"}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Till den andra"}""");

        var messages = await client.GetMessagesAsync(new MessageQuery { To = "+46701740605" }, TestContext.Current.CancellationToken);

        messages.Should().ContainSingle().Which.To.Should().Be("+46701740605");
    }

    [Fact]
    public async Task An_e_mail_address_with_a_plus_finds_its_message()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"email:test.user+orders@example.se","subject":"Your order","text":"Shipped"}""");
        await Send(emulator, """{"to":"email:test.user@example.se","subject":"Your account","text":"Created"}""");

        var messages = await client.GetMessagesAsync(new MessageQuery { To = "test.user+orders@example.se" }, TestContext.Current.CancellationToken);

        messages.Should().ContainSingle().Which.To.Should().Be("test.user+orders@example.se");
    }

    [Fact]
    public async Task A_search_with_characters_a_query_reserves_finds_its_message()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740605","text":"Fish & chips #1 = 100% +tax"}""");
        await Send(emulator, """{"to":"sms:+46701740605","text":"Fish and chips"}""");

        var messages = await client.GetMessagesAsync(new MessageQuery { Search = "Fish & chips #1 = 100% +tax" }, TestContext.Current.CancellationToken);

        messages.Should().ContainSingle().Which.Text.Should().Be("Fish & chips #1 = 100% +tax");
    }

    [Fact]
    public async Task A_search_with_a_non_ASCII_word_finds_its_message()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740605","text":"Välkommen till tjänsten"}""");
        await Send(emulator, """{"to":"sms:+46701740605","text":"Din kod är 482913"}""");

        var messages = await client.GetMessagesAsync(new MessageQuery { Search = "välkommen" }, TestContext.Current.CancellationToken);

        messages.Should().ContainSingle().Which.Text.Should().Be("Välkommen till tjänsten");
    }

    [Fact]
    public async Task Filters_together_narrow_the_list()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740605","text":"Ett sms"}""");
        await Send(emulator, """{"to":"mms:+46701740605","text":"Ett mms"}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Ett annat sms"}""");

        var messages = await client.GetMessagesAsync(
            new MessageQuery { State = MessageState.Sent, Channel = MessageChannel.Sms, To = "+46701740605" },
            TestContext.Current.CancellationToken);

        messages.Should().ContainSingle().Which.Text.Should().Be("Ett sms");
    }

    [Fact]
    public async Task Clearing_removes_every_message()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var cancellation = TestContext.Current.CancellationToken;
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Senare","scheduled_send_time":"2026-11-08T10:00:00Z"}""");

        await client.DeleteMessagesAsync(cancellation);

        (await client.GetMessagesAsync(cancellationToken: cancellation)).Should().BeEmpty();
    }

    [Fact]
    public async Task The_list_maps_every_property_the_read_API_serves()
    {
        await using var emulator = new EmulatorFactory();
        using var http = emulator.CreateClient();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Svara: redirect=https://example.se/quiz","report_to":"https://example.se/reports","reply_to":"email:ops@example.se","note":null}""");
        var strict = new JsonSerializerOptions(ClientJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

        var list = await http.GetFromJsonAsync<MessageList>(new Uri("api/messages", UriKind.Relative), strict, TestContext.Current.CancellationToken);

        list!.Total.Should().Be(1);
    }

    [Fact]
    public async Task An_address_that_serves_no_read_API_fails_the_call()
    {
        await using var emulator = new EmulatorFactory();
        using var http = emulator.CreateClient();
        http.BaseAddress = new Uri("http://localhost/elsewhere/");
        using var client = new TellusTalkEmulatorClient(http);

        var list = () => client.GetMessagesAsync(cancellationToken: TestContext.Current.CancellationToken);

        (await list.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task Send(EmulatorFactory emulator, string json)
    {
        using var response = await emulator.Send(json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
