using System.Net;
using System.Text;
using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Api;

public sealed class ReadApiTests
{
    private const string Welcome =
        "Välkommen till kliniken! Vi har tagit emot din intresseanmälan och en av våra koordinatorer ringer dig inom två " +
        "arbetsdagar. Logga in med e-post för att följa ärendet och se vilka dokument vi behöver från dig.";

    private static readonly string Detailed = JsonSerializer.Serialize(new Dictionary<string, object>(StringComparer.Ordinal)
    {
        ["to"] = "sms:+46 70 174 06 05",
        ["text"] = Welcome,
        ["sms_originator_source"] = "text",
        ["sms_originator_text"] = "Probe",
        ["sms_flash"] = true,
        ["sms_orginator_text"] = "typo",
        ["report_to"] = "https://example.se/reports",
    });

    [Fact]
    public async Task Messages_are_listed_newest_first()
    {
        await using var emulator = new EmulatorFactory();
        await SendTwo(emulator);

        using var list = await Get(emulator, "/api/v1/messages");

        var root = list.RootElement;
        root.GetProperty("total").GetInt32().Should().Be(2);
        root.GetProperty("messages")[0].GetProperty("text").GetString().Should().Be(Welcome);
        root.GetProperty("messages")[1].GetProperty("text").GetString().Should().Be("Först");
    }

    [Fact]
    public async Task A_message_carries_its_ids_recipient_user_and_times()
    {
        await using var emulator = new EmulatorFactory();
        var newest = await SendTwo(emulator);

        newest.GetProperty("id").GetString().Should().Be(newest.GetProperty("jobId").GetString());
        newest.GetProperty("state").GetString().Should().Be("sent");
        newest.GetProperty("to").GetString().Should().Be("+46701740605");
        newest.GetProperty("toAsSent").GetString().Should().Be("sms:+46 70 174 06 05");
        newest.GetProperty("notCaught").GetArrayLength().Should().Be(0);
        newest.GetProperty("user").GetString().Should().Be("tester");
        newest.GetProperty("receivedAt").GetDateTimeOffset().Should().Be(new DateTimeOffset(2026, 11, 7, 22, 1, 0, TimeSpan.Zero));
        newest.GetProperty("dueAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task A_message_carries_its_sender_text_and_parts()
    {
        await using var emulator = new EmulatorFactory();
        var newest = await SendTwo(emulator);

        var sender = newest.GetProperty("sender");
        sender.GetProperty("shown").GetString().Should().Be("Probe");
        sender.GetProperty("kind").GetString().Should().Be("name");
        sender.GetProperty("source").GetString().Should().Be("text");
        sender.GetProperty("fromAccount").GetBoolean().Should().BeFalse();

        newest.GetProperty("text").GetString().Should().Be(Welcome);
        newest.GetProperty("links").GetArrayLength().Should().Be(0);

        var parts = newest.GetProperty("parts");
        parts.GetProperty("characterSet").GetString().Should().Be("gsm");
        parts.GetProperty("count").GetInt32().Should().Be(2);
        parts.GetProperty("length").GetInt32().Should().Be(209);
        parts.GetProperty("starts")
            .EnumerateArray()
            .Select(start => start.GetInt32())
            .Should().Equal(0, 153);
        parts.GetProperty("leftInLastPart").GetInt32().Should().Be(97);
        parts.GetProperty("maxParts").GetInt32().Should().Be(8);
    }

    [Fact]
    public async Task A_scheduled_state_Unicode_a_number_sender_and_e_mail_and_SMS_addresses_are_written_in_lower_case()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej 😀","sms_originator_source":"number","sms_originator_number":"+46701740699","report_to":["email:ops@example.se","sms:+46701740698"],"scheduled_send_time":"2026-11-08T10:00:00Z"}""");

        using var message = await Get(emulator, $"/api/v1/messages/{id}");

        var root = message.RootElement;
        root.GetProperty("state").GetString().Should().Be("scheduled");
        root.GetProperty("parts").GetProperty("characterSet").GetString().Should().Be("ucs2");
        root.GetProperty("sender").GetProperty("kind").GetString().Should().Be("number");
        root.GetProperty("reportTo")
            .EnumerateArray()
            .Select(address => address.GetProperty("kind").GetString())
            .Should().Equal("email", "sms");
    }

    [Fact]
    public async Task A_message_shows_its_tracking_links_shortened_and_lists_where_each_leads()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Svara här: redirect=https://example.se/quiz eller läs --LINK_VIEW--"}""");

        using var message = await Get(emulator, $"/api/v1/messages/{id}");

        var root = message.RootElement;
        root.GetProperty("text").GetString().Should().Be("Svara här: https://ebox.nu/r_xxxxxx/0 eller läs https://ebox.nu/z_xxxxxx");
        var links = root
            .GetProperty("links")
            .EnumerateArray()
            .ToList();
        links.Should().HaveCount(2);
        links[0].GetProperty("kind").GetString().Should().Be("redirect");
        links[0].GetProperty("shown").GetString().Should().Be("https://ebox.nu/r_xxxxxx/0");
        links[0].GetProperty("target").GetString().Should().Be("https://example.se/quiz");
        links[1].GetProperty("kind").GetString().Should().Be("view");
        links[1].GetProperty("shown").GetString().Should().Be("https://ebox.nu/z_xxxxxx");
        links[1].GetProperty("target").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task A_binary_character_set_is_written_in_lower_case()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"0102","sms_character_set":"binary","sms_udh":"050003000101"}""");

        using var message = await Get(emulator, $"/api/v1/messages/{id}");

        message.RootElement.GetProperty("parts").GetProperty("characterSet").GetString().Should().Be("binary");
    }

    [Fact]
    public async Task A_message_carries_its_addresses_fields_warnings_and_request()
    {
        await using var emulator = new EmulatorFactory();
        var newest = await SendTwo(emulator);

        var reportTo = newest
            .GetProperty("reportTo")
            .EnumerateArray()
            .Single();
        reportTo.GetProperty("kind").GetString().Should().Be("url");
        reportTo.GetProperty("target").GetString().Should().Be("https://example.se/reports");
        newest.GetProperty("replyTo").GetArrayLength().Should().Be(0);

        newest.GetProperty("fields").GetProperty("sms_flash").GetBoolean().Should().BeTrue();

        var codes = newest.GetProperty("warnings")
            .EnumerateArray()
            .Select(warning => warning.GetProperty("code").GetString())
            .ToList();
        codes.Should().BeEquivalentTo(["to-spaces-removed", "unknown-field"]);

        newest.GetProperty("request").GetString().Should().Be(Detailed);
    }

    [Fact]
    public async Task A_message_names_its_requests_recipients_on_other_channels_as_sent()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":["sms:+46701740605","email:test.user@example.se","fax:+46701740606"],"text":"Hej"}""");

        using var list = await Get(emulator, "/api/v1/messages");

        list.RootElement.GetProperty("messages")[0].GetProperty("notCaught")
            .EnumerateArray()
            .Select(address => address.GetString())
            .Should().Equal("email:test.user@example.se", "fax:+46701740606");
    }

    [Fact]
    public async Task A_scheduled_message_is_listed_as_scheduled_until_it_falls_due()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Senare","scheduled_send_time":"2026-11-07T23:00:00Z"}""");

        (await Total(emulator, "/api/v1/messages?state=scheduled")).Should().Be(1);
        (await Total(emulator, "/api/v1/messages?state=sent")).Should().Be(0);

        emulator.Time.Advance(TimeSpan.FromHours(2));

        (await Total(emulator, "/api/v1/messages?state=sent")).Should().Be(1);
    }

    [Fact]
    public async Task A_due_time_is_served_in_UTC_like_every_other_time()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Senare","scheduled_send_time":"2026-11-08T00:30:00"}""");

        using var list = await Get(emulator, "/api/v1/messages");

        list.RootElement.GetProperty("messages")[0].GetProperty("dueAt").GetString().Should().Be("2026-11-07T23:30:00+00:00");
    }

    [Fact]
    public async Task Messages_filter_by_recipient_even_with_an_unescaped_plus()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Hej"}""");

        (await Total(emulator, "/api/v1/messages?to=%2B46701740605")).Should().Be(1);
        (await Total(emulator, "/api/v1/messages?to=+46701740605")).Should().Be(1);
    }

    [Fact]
    public async Task Messages_filter_by_a_search()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, JsonSerializer.Serialize(new { to = "sms:+46701740605", text = Welcome }));
        await Send(emulator, """{"to":"sms:+46701740605","text":"Din kod är 482913"}""");

        (await Total(emulator, "/api/v1/messages?q=v%C3%A4lkommen")).Should().Be(1);
    }

    [Fact]
    public async Task An_unknown_state_is_refused()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/messages?state=gone", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_unknown_message_is_not_found()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        var cancellation = TestContext.Current.CancellationToken;

        using var get = await client.GetAsync(new Uri("/api/v1/messages/nothing", UriKind.Relative), cancellation);
        using var delete = await client.DeleteAsync(new Uri("/api/v1/messages/nothing", UriKind.Relative), cancellation);

        get.StatusCode.Should().Be(HttpStatusCode.NotFound);
        delete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_message_is_read_and_deleted_by_its_id()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        var cancellation = TestContext.Current.CancellationToken;
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        using (var message = await Get(emulator, $"/api/v1/messages/{id}"))
        {
            message.RootElement.GetProperty("text").GetString().Should().Be("Hej");
        }

        using var delete = await client.DeleteAsync(new Uri($"/api/v1/messages/{id}", UriKind.Relative), cancellation);

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Total(emulator, "/api/v1/messages")).Should().Be(0);
    }

    [Fact]
    public async Task Every_message_is_deleted_at_once()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Hej"}""");

        using var delete = await client.DeleteAsync(new Uri("/api/v1/messages", UriKind.Relative), TestContext.Current.CancellationToken);

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await Total(emulator, "/api/v1/messages")).Should().Be(0);
    }

    [Fact]
    public async Task Armed_send_failures_are_read_as_they_are_spent_and_cleared()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        var cancellation = TestContext.Current.CancellationToken;
        using var order = new StringContent("""{"status":503,"count":2}""", Encoding.UTF8, "application/json");

        using var armed = await client.PutAsync(new Uri("/api/v1/send-failures", UriKind.Relative), order, cancellation);
        (await armed.Content.ReadAsStringAsync(cancellation)).Should().Be("""{"status":503,"remaining":2,"delaySeconds":0}""");

        using (await emulator.Send("""{"to":"sms:+46701740605","text":"Hej"}"""))
        {
            (await GetText(client, "/api/v1/send-failures")).Should().Be("""{"status":503,"remaining":1,"delaySeconds":0}""");
        }

        using var cleared = await client.DeleteAsync(new Uri("/api/v1/send-failures", UriKind.Relative), cancellation);

        cleared.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetText(client, "/api/v1/send-failures")).Should().Be("""{"status":null,"remaining":0,"delaySeconds":null}""");
    }

    [Fact]
    public async Task Deleting_every_message_leaves_armed_send_failures_as_they_are()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        var cancellation = TestContext.Current.CancellationToken;
        using var order = new StringContent("""{"status":503,"delaySeconds":30}""", Encoding.UTF8, "application/json");
        using var armed = await client.PutAsync(new Uri("/api/v1/send-failures", UriKind.Relative), order, cancellation);

        using var delete = await client.DeleteAsync(new Uri("/api/v1/messages", UriKind.Relative), cancellation);

        (await GetText(client, "/api/v1/send-failures")).Should().Be("""{"status":503,"remaining":1,"delaySeconds":30}""");
    }

    [Fact]
    public async Task The_event_stream_announces_a_send()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));

#pragma warning disable CA2025 // The reading task is awaited below, before the client and the timeout are disposed.
        var reading = ReadFirstEvent(client, timeout.Token);
#pragma warning restore CA2025

        // The stream subscribes when its request reaches the endpoint, so a send is repeated until one is seen.
        while (!reading.IsCompleted)
        {
            await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
            await Task.WhenAny(reading, Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token));
        }

        var (eventType, data) = await reading;
        eventType.Should().Be("change");
        data.Should().Be("""{"kind":"added"}""");
    }

    private static async Task<(string EventType, string Data)> ReadFirstEvent(HttpClient client, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(
            new Uri("/api/v1/events", UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? eventType = null;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventType = line["event: ".Length..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                return (eventType!, line["data: ".Length..]);
            }
        }

        throw new InvalidOperationException("The event stream ended without an event.");
    }

    /// <summary>Sends a plain message, then a minute later the detailed one, and returns the detailed one as listed.</summary>
    private static async Task<JsonElement> SendTwo(EmulatorFactory emulator)
    {
        await Send(emulator, """{"to":"sms:+46701740606","text":"Först"}""");
        emulator.Time.Advance(TimeSpan.FromMinutes(1));
        await Send(emulator, Detailed);

        using var list = await Get(emulator, "/api/v1/messages");

        return list.RootElement.GetProperty("messages")[0].Clone();
    }

    private static async Task<string> Send(EmulatorFactory emulator, string json)
    {
        using var response = await emulator.Send(json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return answer.RootElement.GetProperty("job_id").GetString()!;
    }

    private static async Task<JsonDocument> Get(EmulatorFactory emulator, string path)
    {
        using var client = emulator.CreateClient();
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<string> GetText(HttpClient client, string path)
    {
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<int> Total(EmulatorFactory emulator, string path)
    {
        using var list = await Get(emulator, path);

        return list.RootElement.GetProperty("total").GetInt32();
    }
}
