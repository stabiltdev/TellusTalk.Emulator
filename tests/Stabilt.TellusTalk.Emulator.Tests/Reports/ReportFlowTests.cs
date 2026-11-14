using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Reports;

public sealed class ReportFlowTests
{
    private const string WithReport = """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""";

    private static readonly DateTimeOffset Start = new(2026, 11, 7, 22, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task A_report_follows_a_send_once_the_delay_has_passed()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Settings["Emulator:ReportDelaySeconds"] = "5";
        var id = await Send(emulator, WithReport);

        var report = (await StepUntilReported(emulator, 1)).Single();

        using var body = JsonDocument.Parse(report.Body);
        body.RootElement.GetProperty("log_status").GetString().Should().Be("txtDELIVERED");
        body.RootElement.GetProperty("log_uuid").GetString().Should().Be(id);
        ReportTime(body).Should().BeOnOrAfter(Start.AddSeconds(5));
    }

    [Fact]
    public async Task No_number_fails_by_default()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740699","text":"Hej","report_to":"https://example.se/reports"}""");

        using var body = JsonDocument.Parse((await StepUntilReported(emulator, 1)).Single().Body);
        body.RootElement.GetProperty("log_status").GetString().Should().Be("txtDELIVERED");
    }

    [Fact]
    public async Task The_failing_numbers_are_a_setting()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Settings["Emulator:FailingNumbers"] = "+46701740606, +46701740605";
        await Send(emulator, WithReport);

        using var body = JsonDocument.Parse((await StepUntilReported(emulator, 1)).Single().Body);
        body.RootElement.GetProperty("log_status").GetString().Should().Be("txtDELIVERY_FAILED");
    }

    [Fact]
    public async Task A_scheduled_message_is_reported_after_it_falls_due()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports","scheduled_send_time":"2026-11-07T22:10:00Z"}""");

        emulator.Time.Advance(TimeSpan.FromMinutes(10));

        using var body = JsonDocument.Parse((await StepUntilReported(emulator, 1)).Single().Body);
        ReportTime(body).Should().BeOnOrAfter(Start.AddMinutes(10).AddSeconds(1));
    }

    [Fact]
    public async Task A_receiver_that_never_answers_holds_up_no_other_report()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Reports.SilentHosts.Add("silent.example.se");
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":["https://silent.example.se/reports","https://example.se/first"]}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Hej","report_to":"https://example.se/second"}""");

        var received = await StepUntilReported(emulator, 3);

        received
            .Select(report => report.Uri.AbsolutePath)
            .Should().Contain(["/first", "/second"]);
    }

    [Fact]
    public async Task A_message_carries_its_report_attempts_in_the_read_API()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, WithReport);
        await StepUntilReported(emulator, 1);

        var reports = await Attempts(emulator, id, expected: 1);

        var attempt = reports.Single();
        attempt.GetProperty("status").GetString().Should().Be("delivered");
        attempt.GetProperty("target").GetString().Should().Be("https://example.se/reports");
        attempt.GetProperty("sent").GetBoolean().Should().BeTrue();
        attempt.GetProperty("outcome").GetString().Should().Be("200 OK");
    }

    [Fact]
    public async Task A_report_is_sent_on_request_with_the_status_asked_for()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, WithReport);
        using var client = emulator.CreateClient();

        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/messages/{id}/reports", UriKind.Relative),
            new { status = "failed" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(emulator.Reports.Received.Should().ContainSingle().Subject.Body);
        body.RootElement.GetProperty("log_status").GetString().Should().Be("txtDELIVERY_FAILED");
    }

    [Fact]
    public async Task A_report_on_request_is_kept_when_the_caller_stops_waiting()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, WithReport);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        emulator.Reports.Gate = gate;
        using var client = emulator.CreateClient();
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

#pragma warning disable CA2025 // The request is awaited below, before the client and the caller's token are disposed.
        var request = client.PostAsJsonAsync(new Uri($"/api/v1/messages/{id}/reports", UriKind.Relative), new { status = "failed" }, caller.Token);
#pragma warning restore CA2025
        await emulator.Reports.WaitFor(1);
        await caller.CancelAsync();
        await FluentActions
            .Awaiting(() => request)
            .Should().ThrowAsync<OperationCanceledException>();
        gate.SetResult();

        (await Attempts(emulator, id, expected: 1)).Single().GetProperty("status").GetString().Should().Be("failed");
    }

    [Fact]
    public async Task A_report_on_request_for_an_unknown_message_or_status_is_refused()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, WithReport);
        using var client = emulator.CreateClient();
        var cancellation = TestContext.Current.CancellationToken;

        using var unknownMessage = await client.PostAsJsonAsync(new Uri("/api/v1/messages/nothing/reports", UriKind.Relative), new { status = "failed" }, cancellation);
        using var unknownStatus = await client.PostAsJsonAsync(new Uri($"/api/v1/messages/{id}/reports", UriKind.Relative), new { status = "lost" }, cancellation);

        unknownMessage.StatusCode.Should().Be(HttpStatusCode.NotFound);
        unknownStatus.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_message_without_a_report_address_gets_no_report()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        for (var step = 0; step < 5; step++)
        {
            await Step(emulator);
        }

        emulator.Reports.Received.Should().BeEmpty();
        (await Attempts(emulator, id, expected: 0)).Should().BeEmpty();
    }

    private static async Task<string> Send(EmulatorFactory emulator, string json)
    {
        using var response = await emulator.Send(json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return answer.RootElement.GetProperty("job_id").GetString()!;
    }

    /// <summary>
    /// Moves the clock a second at a time until <paramref name="count"/> reports have arrived. The report service's timer
    /// may start after the test has moved the clock, so a single jump could pass before its first tick.
    /// </summary>
    private static async Task<IReadOnlyList<ReceivedReport>> StepUntilReported(EmulatorFactory emulator, int count)
    {
        for (var step = 0; step < 40; step++)
        {
            if (emulator.Reports.Received.Count >= count)
            {
                return [.. emulator.Reports.Received];
            }

            await Step(emulator);
        }

        throw new TimeoutException($"{emulator.Reports.Received.Count} of {count} reports arrived within 40 seconds of the emulator's time.");
    }

    private static Task Step(EmulatorFactory emulator)
    {
        emulator.Time.Advance(TimeSpan.FromSeconds(1));

        return Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
    }

    private static DateTimeOffset ReportTime(JsonDocument report) =>
        DateTimeOffset.Parse(report.RootElement.GetProperty("report_time").GetString()!, CultureInfo.InvariantCulture);

    /// <summary>The message's report attempts, read again until <paramref name="expected"/> are recorded.</summary>
    private static async Task<IReadOnlyList<JsonElement>> Attempts(EmulatorFactory emulator, string id, int expected)
    {
        using var client = emulator.CreateClient();
        for (var attempt = 0; attempt < 50; attempt++)
        {
            using var message = JsonDocument.Parse(await client.GetStringAsync(new Uri($"/api/v1/messages/{id}", UriKind.Relative), TestContext.Current.CancellationToken));
            var reports = message.RootElement
                .GetProperty("reports")
                .EnumerateArray()
                .Select(report => report.Clone())
                .ToList();

            if (reports.Count >= expected)
            {
                return reports;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"Fewer than {expected} report attempts were recorded.");
    }
}
