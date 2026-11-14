using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Testcontainers.Tests;

public sealed class TellusTalkEmulatorBuilderTests
{
    [Fact]
    public async Task The_emulator_runs_the_published_image_of_the_packages_own_version()
    {
        // Building needs a Docker endpoint but no Docker that answers: the container is never started.
        await using var emulator = new TellusTalkEmulatorBuilder()
            .WithDockerEndpoint("tcp://127.0.0.1:2375")
            .Build();

        var version = typeof(TellusTalkEmulatorBuilder).Assembly.GetName().Version!.ToString(3);
        emulator.Image.FullName.Should().Be($"ghcr.io/stabiltdev/tellustalk-emulator:{version}");
    }

    [Fact]
    public async Task A_started_emulator_takes_a_send_at_its_base_address_and_serves_it_back()
    {
        await using var emulator = LocalImage.Builder().Build();
        await emulator.StartAsync(TestContext.Current.CancellationToken);
        using var client = Client(emulator);

        await Send(client, """{"to": "sms:+46701740605", "text": "Your code is 482913"}""");

        var message = await OnlyMessage(client);
        message.GetProperty("to").GetString().Should().Be("+46701740605");
        message.GetProperty("text").GetString().Should().Be("Your code is 482913");
    }

    [Fact]
    public async Task A_send_without_a_sender_shows_the_account_sender()
    {
        await using var emulator = LocalImage.Builder()
            .WithAccountSender("Acme")
            .Build();
        await emulator.StartAsync(TestContext.Current.CancellationToken);
        using var client = Client(emulator);

        await Send(client, """{"to": "sms:+46701740605", "text": "Your order has shipped."}""");

        var message = await OnlyMessage(client);
        message.GetProperty("sender").GetProperty("shown").GetString().Should().Be("Acme");
    }

    [Fact]
    public async Task A_failing_number_gets_a_failure_report()
    {
        await using var emulator = LocalImage.Builder()
            .WithFailingNumbers("+46701740698")
            .WithReportDelay(TimeSpan.Zero)
            .Build();
        await emulator.StartAsync(TestContext.Current.CancellationToken);
        using var client = Client(emulator);

        await Send(client, """{"to": "sms:+46701740698", "text": "Your order has shipped.", "report_to": "reports@example.se"}""");

        var report = (await Reported(client)).GetProperty("reports")[0];
        report.GetProperty("status").GetString().Should().Be("failed");
    }

    [Fact]
    public async Task Failing_numbers_from_several_calls_add_up()
    {
        await using var emulator = LocalImage.Builder()
            .WithFailingNumbers("+46701740697")
            .WithFailingNumbers("+46701740698")
            .WithReportDelay(TimeSpan.Zero)
            .Build();
        await emulator.StartAsync(TestContext.Current.CancellationToken);
        using var client = Client(emulator);

        await Send(client, """{"to": "sms:+46701740697", "text": "Your order has shipped.", "report_to": "reports@example.se"}""");

        var report = (await Reported(client)).GetProperty("reports")[0];
        report.GetProperty("status").GetString().Should().Be("failed");
    }

    [Fact]
    public async Task A_report_waits_for_the_report_delay()
    {
        await using var emulator = LocalImage.Builder()
            .WithReportDelay(TimeSpan.FromSeconds(3))
            .Build();
        await emulator.StartAsync(TestContext.Current.CancellationToken);
        using var client = Client(emulator);

        await Send(client, """{"to": "sms:+46701740605", "text": "Your order has shipped.", "report_to": "reports@example.se"}""");

        var message = await Reported(client);
        var receivedAt = message.GetProperty("receivedAt").GetDateTimeOffset();
        var reportedAt = message.GetProperty("reports")[0].GetProperty("at").GetDateTimeOffset();
        (reportedAt - receivedAt).Should().BeGreaterThanOrEqualTo(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task The_emulator_keeps_no_more_messages_than_the_retention_limit()
    {
        await using var emulator = LocalImage.Builder()
            .WithRetentionLimit(1)
            .Build();
        await emulator.StartAsync(TestContext.Current.CancellationToken);
        using var client = Client(emulator);

        await Send(client, """{"to": "sms:+46701740605", "text": "First"}""");
        await Send(client, """{"to": "sms:+46701740605", "text": "Second"}""");

        var message = await OnlyMessage(client);
        message.GetProperty("text").GetString().Should().Be("Second");
    }

    private static HttpClient Client(TellusTalkEmulatorContainer emulator)
    {
        var client = new HttpClient { BaseAddress = new Uri(emulator.GetBaseAddress()) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String("tester:secret"u8));

        return client;
    }

    private static async Task Send(HttpClient client, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("send/v1", UriKind.Relative), content, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private static async Task<JsonElement> OnlyMessage(HttpClient client)
    {
        var list = await client.GetFromJsonAsync<JsonElement>(new Uri("api/v1/messages", UriKind.Relative), TestContext.Current.CancellationToken);

        return list.GetProperty("messages").EnumerateArray().Should().ContainSingle().Subject;
    }

    /// <summary>The only message once its first delivery report has gone out; the emulator reports on a one-second tick.</summary>
    private static async Task<JsonElement> Reported(HttpClient client)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            var message = await OnlyMessage(client);
            if (message.GetProperty("reports").GetArrayLength() > 0)
            {
                return message;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken);
        }

        Assert.Fail("No delivery report went out within ten seconds.");
        return default;
    }
}
