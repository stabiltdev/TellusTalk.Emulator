using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Client.Tests;

public sealed class CaughtMessageTests
{
    private const string Detailed = """{"to":"sms:+46 70 174 06 05","text":"Your order has shipped.","sms_originator_source":"text","sms_originator_text":"Probe","sms_max_parts":4,"sms_flash":true,"echo":"order-1001","note":null,"report_to":"https:{\"headers\":{\"Authorization\":\"Basic dGVzdA==\"}}://example.se/reports","reply_to":"email:ops@example.se"}""";

    private const string EveryPart = """{"to":"sms:+46701740605","text":"Svara: redirect=https://example.se/quiz","report_to":"https://example.se/reports","reply_to":"email:ops@example.se","note":null}""";

    [Fact]
    public async Task An_sms_is_read_with_every_property_the_read_API_serves()
    {
        await using var emulator = new EmulatorFactory();
        using var http = emulator.CreateClient();
        using var client = new TellusTalkEmulatorClient(http);
        var id = await Send(emulator, Detailed);
        await Report(http, id, "delivered");
        var at = new DateTimeOffset(2026, 11, 7, 22, 0, 0, TimeSpan.Zero);

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.Id.Should().Be(id);
        message.JobId.Should().Be(id);
        message.State.Should().Be(MessageState.Sent);
        message.DuplicateOf.Should().BeNull();
        message.Channel.Should().Be(MessageChannel.Sms);
        message.To.Should().Be("+46701740605");
        message.ToAsSent.Should().Be("sms:+46 70 174 06 05");
        message.User.Should().Be("tester");
        message.ReceivedAt.Should().Be(at);
        message.DueAt.Should().BeNull();
        message.Sender.Should().BeEquivalentTo(new { Shown = "Probe", Kind = SenderKind.Name, Source = "text", FromAccount = false });
        message.Text.Should().Be("Your order has shipped.");
        message.Links.Should().BeEmpty();
        message.Parts.Should().BeEquivalentTo(new { CharacterSet = SmsCharacterSet.Gsm, Count = 1, Length = 23, LeftInLastPart = 137, MaxParts = 4 });
        message.Parts!.Starts.Should().Equal(0);
        message.ReportTo.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Kind = ReportAddressKind.Url, Target = "https://example.se/reports", ContentType = (string?)null, Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["Authorization"] = "Basic dGVzdA==" } });
        message.ReplyTo.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Kind = ReportAddressKind.Email, Target = "ops@example.se" });
        message.Fields["echo"].GetString().Should().Be("order-1001");
        message.Fields["sms_flash"].ValueKind.Should().Be(JsonValueKind.True);
        message.Fields["sms_max_parts"].GetInt32().Should().Be(4);
        message.Fields["note"].ValueKind.Should().Be(JsonValueKind.Null);
        message.Warnings
            .Select(warning => warning.Code)
            .Should().BeEquivalentTo(["to-spaces-removed", "unknown-field"]);
        message.Warnings.Should().ContainSingle(warning => warning.Code == "unknown-field").Which.Field.Should().Be("note");
        message.Reports.Should().ContainSingle().Which.Should().BeEquivalentTo(new { At = at, Status = ReportStatus.Delivered, Target = "https://example.se/reports", Sent = true, Outcome = "200 OK" });
        message.Request.Should().Be(Detailed);
    }

    [Fact]
    public async Task A_scheduled_message_is_read_with_its_due_time()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Later","scheduled_send_time":"2026-11-08T10:00:00Z"}""");

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.State.Should().Be(MessageState.Scheduled);
        message.DueAt.Should().Be(new DateTimeOffset(2026, 11, 8, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task A_held_back_message_is_read_as_blocked_with_the_message_it_duplicates()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var cancellation = TestContext.Current.CancellationToken;
        var first = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        var second = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        var original = await client.GetMessageAsync(first, cancellation);
        var held = await client.GetMessageAsync(second, cancellation);

        held!.State.Should().Be(MessageState.Blocked);
        held.DuplicateOf.Should().Be(first);
        original!.DuplicateOf.Should().BeNull();
    }

    [Fact]
    public async Task A_held_back_message_reads_its_reason()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var cancellation = TestContext.Current.CancellationToken;
        var first = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        var second = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        var original = await client.GetMessageAsync(first, cancellation);
        var held = await client.GetMessageAsync(second, cancellation);

        held!.BlockedReason.Should().Be(BlockedReason.Duplicate);
        original!.BlockedReason.Should().BeNull();
    }

    [Fact]
    public async Task A_message_blocked_for_its_number_reads_its_reason()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var id = await Send(emulator, """{"to":"sms:+4670174062X","text":"Hej"}""");

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.State.Should().Be(MessageState.Blocked);
        message.BlockedReason.Should().Be(BlockedReason.NumberNotNumeric);
    }

    [Fact]
    public async Task A_message_on_another_channel_is_read_without_sender_links_or_parts()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var id = await Send(emulator, """{"to":"email:test.user@example.se","subject":"Your order","text":"Shipped"}""");

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.Channel.Should().Be(MessageChannel.Email);
        message.To.Should().Be("test.user@example.se");
        message.Text.Should().Be("Shipped");
        message.Sender.Should().BeNull();
        message.Links.Should().BeNull();
        message.Parts.Should().BeNull();
        message.Reports.Should().BeEmpty();
        message.Fields["subject"].GetString().Should().Be("Your order");
    }

    [Theory]
    [InlineData("sms:+46701740606", MessageChannel.Sms)]
    [InlineData("email:test.user@example.se", MessageChannel.Email)]
    [InlineData("fax:+46701740606", MessageChannel.Fax)]
    [InlineData("mms:+46701740606", MessageChannel.Mms)]
    [InlineData("voice:+46701740606", MessageChannel.Voice)]
    [InlineData("group:box.abc123", MessageChannel.Group)]
    public async Task Every_channel_is_read(string address, MessageChannel channel)
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var id = await Send(emulator, JsonSerializer.Serialize(new { to = address, text = "Hej" }));

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.Channel.Should().Be(channel);
    }

    [Theory]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej"}""", SmsCharacterSet.Gsm)]
    [InlineData("""{"to":"sms:+46701740605","text":"Hej 😀"}""", SmsCharacterSet.Ucs2)]
    [InlineData("""{"to":"sms:+46701740605","text":"0102","sms_character_set":"binary","sms_udh":"050003000101"}""", SmsCharacterSet.Binary)]
    public async Task Every_character_set_is_read(string json, SmsCharacterSet characterSet)
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var id = await Send(emulator, json);

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.Parts!.CharacterSet.Should().Be(characterSet);
    }

    [Fact]
    public async Task Both_sender_kinds_are_read()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var cancellation = TestContext.Current.CancellationToken;
        var fromAccount = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        var named = await Send(emulator, """{"to":"sms:+46701740606","text":"Hej","subject":"Acme"}""");

        var number = await client.GetMessageAsync(fromAccount, cancellation);
        var name = await client.GetMessageAsync(named, cancellation);

        number!.Sender.Should().BeEquivalentTo(new { Shown = "+46701740698", Kind = SenderKind.Number, Source = "account", FromAccount = true });
        name!.Sender.Should().BeEquivalentTo(new { Kind = SenderKind.Name, Source = "subject" });
    }

    [Fact]
    public async Task Both_link_kinds_are_read()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Svara: redirect=https://example.se/quiz eller --LINK_VIEW--"}""");

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.Links.Should().Equal(
            new TrackingLink { Kind = TrackingLinkKind.Redirect, Shown = "https://ebox.nu/r_xxxxxx/0", Target = "https://example.se/quiz" },
            new TrackingLink { Kind = TrackingLinkKind.View, Shown = "https://ebox.nu/z_xxxxxx", Target = null });
    }

    [Fact]
    public async Task Every_report_address_kind_is_read()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":["email:ops@example.se","sms:+46701740698","https://example.se/reports"]}""");

        var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        message!.ReportTo
            .Select(address => address.Kind)
            .Should().Equal(ReportAddressKind.Email, ReportAddressKind.Sms, ReportAddressKind.Url);
        message.ReportTo
            .Select(address => address.Target)
            .Should().Equal("ops@example.se", "+46701740698", "https://example.se/reports");
    }

    [Fact]
    public async Task Every_report_status_is_read()
    {
        await using var emulator = new EmulatorFactory();
        using var http = emulator.CreateClient();
        using var client = new TellusTalkEmulatorClient(http);
        var cancellation = TestContext.Current.CancellationToken;
        const string json = """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""";
        var id = await Send(emulator, json);
        await Report(http, id, "delivered");
        await Report(http, id, "failed");
        var held = await Send(emulator, json);

        var reported = await client.GetMessageAsync(id, cancellation);
        var blocked = await ReadOnceReported(client, held);

        reported!.Reports
            .Select(report => report.Status)
            .Should().Equal(ReportStatus.Delivered, ReportStatus.Failed);
        blocked.Reports.Should().ContainSingle().Which.Status.Should().Be(ReportStatus.Blocked);
    }

    [Fact]
    public async Task The_models_map_every_property_the_read_API_serves()
    {
        await using var emulator = new EmulatorFactory();
        using var http = emulator.CreateClient();
        var id = await Send(emulator, EveryPart);
        await Report(http, id, "delivered");
        var strict = new JsonSerializerOptions(ClientJson.Options) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

        var message = await http.GetFromJsonAsync<CaughtMessage>(new Uri($"api/messages/{id}", UriKind.Relative), strict, TestContext.Current.CancellationToken);

        message.Should().NotBeNull();
    }

    [Fact]
    public async Task An_unknown_id_is_read_as_null()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());

        var message = await client.GetMessageAsync("nothing", TestContext.Current.CancellationToken);

        message.Should().BeNull();
    }

    [Fact]
    public async Task An_id_is_escaped_into_one_path_segment()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        var message = await client.GetMessageAsync("../messages", TestContext.Current.CancellationToken);

        message.Should().BeNull();
    }

    [Fact]
    public async Task An_empty_id_is_refused()
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());

        var read = () => client.GetMessageAsync("", TestContext.Current.CancellationToken);

        await read.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public async Task A_dot_segment_as_an_id_is_refused(string id)
    {
        await using var emulator = new EmulatorFactory();
        using var client = new TellusTalkEmulatorClient(emulator.CreateClient());
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        var read = () => client.GetMessageAsync(id, TestContext.Current.CancellationToken);

        await read.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>The message once a report has reached it: a held-back send's report is posted in the background.</summary>
    private static async Task<CaughtMessage> ReadOnceReported(TellusTalkEmulatorClient client, string id)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            var message = await client.GetMessageAsync(id, TestContext.Current.CancellationToken);
            if (message!.Reports.Count > 0)
            {
                return message;
            }

            if (clock.Elapsed > TimeSpan.FromSeconds(5))
            {
                throw new TimeoutException($"Message {id} had no report within five seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
        }
    }

    private static async Task<string> Send(EmulatorFactory emulator, string json)
    {
        using var response = await emulator.Send(json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return answer.RootElement.GetProperty("job_id").GetString()!;
    }

    private static async Task Report(HttpClient http, string id, string status)
    {
        using var content = new StringContent($$"""{"status":"{{status}}"}""", Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(new Uri($"api/messages/{id}/reports", UriKind.Relative), content, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
