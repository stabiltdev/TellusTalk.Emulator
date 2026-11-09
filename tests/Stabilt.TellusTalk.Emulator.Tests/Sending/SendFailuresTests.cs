using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Sending;

public sealed class SendFailuresTests
{
    private const string Probe = """{"to":"sms:+46701740605","text":"Probe 1"}""";

    [Fact]
    public async Task Armed_failures_answer_the_next_sends_in_order_and_then_let_sends_through()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 503, count = 2 }, new { status = 429 });

        using var first = await emulator.Send(Probe);
        using var second = await emulator.Send(Probe);
        using var third = await emulator.Send(Probe);
        using var fourth = await emulator.Send(Probe);

        first.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        second.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        fourth.StatusCode.Should().Be(HttpStatusCode.OK);
        Store(emulator).List().Should().ContainSingle();
    }

    [Fact]
    public async Task A_failure_is_one_send_answered_500_with_its_reason_phrase_as_plain_text_and_TellusTalks_headers()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { });

        using var failed = await emulator.Send(Probe);
        using var next = await emulator.Send(Probe);

        failed.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        failed.Content.Headers.ContentType!.ToString().Should().Be("text/plain; charset=utf-8");
        (await failed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Internal Server Error");
        failed.Headers.Contains("X-Request-Id").Should().BeTrue();
        next.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_order_without_a_body_arms_one_failure_with_500()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();

        using var armed = await client.PutAsync(new Uri("/api/v1/send-failures", UriKind.Relative), content: null, TestContext.Current.CancellationToken);
        using var failed = await emulator.Send(Probe);

        armed.StatusCode.Should().Be(HttpStatusCode.OK);
        failed.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task A_send_without_credentials_is_refused_before_a_failure_is_spent()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 502 });

        using var anonymous = await emulator.Send(Probe, user: null);
        using var authenticated = await emulator.Send(Probe);

        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        authenticated.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task A_4xx_failure_answers_with_its_reason_phrase_as_plain_text()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 429 });

        using var failed = await emulator.Send(Probe);

        failed.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        failed.Content.Headers.ContentType!.ToString().Should().Be("text/plain; charset=utf-8");
        (await failed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Too Many Requests");
    }

    [Fact]
    public async Task An_armed_401_answers_as_TellusTalk_refuses_a_send_without_credentials()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 401 });

        using var failed = await emulator.Send(Probe);
        using var refused = await emulator.Send(Probe, user: null);

        failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        failed.Content.Headers.ContentType!.ToString().Should().Be(refused.Content.Headers.ContentType!.ToString());
        (await failed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Be(await refused.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        failed.Headers.WwwAuthenticate.Should().BeEmpty();
    }

    [Fact]
    public async Task A_late_failure_is_answered_once_its_delay_has_passed()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 503, delaySeconds = 30 });

        var sending = emulator.Send(Probe);
        for (var second = 0; second < 29; second++)
        {
            await Step(emulator);
        }

        sending.IsCompleted.Should().BeFalse();

        for (var step = 0; step < 20 && !sending.IsCompleted; step++)
        {
            await Step(emulator);
        }

        using var failed = await sending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task A_client_that_gives_up_on_a_late_failure_spends_it_and_the_next_send_goes_through()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { delaySeconds = 30 });
        using var client = emulator.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("tester:secret")));
        using var givingUp = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        givingUp.CancelAfter(TimeSpan.FromMilliseconds(200));
        using var content = new StringContent(Probe, Encoding.UTF8, "application/json");

        await FluentActions
            .Awaiting(() => client.PostAsync(new Uri("/send/v1", UriKind.Relative), content, givingUp.Token))
            .Should().ThrowAsync<OperationCanceledException>();
        using var next = await emulator.Send(Probe);

        next.StatusCode.Should().Be(HttpStatusCode.OK);
        Store(emulator).List().Should().ContainSingle();
    }

    [Fact]
    public async Task Failures_armed_for_a_number_answer_only_sends_to_that_number()
    {
        await using var emulator = new EmulatorFactory();
        await ArmFor(emulator, "+46701740605", new { status = 503 });

        using var other = await emulator.Send(To("+46701740606"));
        using var failed = await emulator.Send(To("+46701740605"));
        using var next = await emulator.Send(To("+46701740605"));

        other.StatusCode.Should().Be(HttpStatusCode.OK);
        failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        next.StatusCode.Should().Be(HttpStatusCode.OK);
        Store(emulator).List()
            .Select(message => message.To.Number)
            .Should().BeEquivalentTo(["+46701740606", "+46701740605"]);
    }

    [Fact]
    public async Task A_number_is_matched_however_the_request_writes_it()
    {
        await using var emulator = new EmulatorFactory();
        await ArmFor(emulator, "+46701740605", new { status = 503 });

        using var failed = await emulator.Send("""{"to":"sms:+46 70 174 06 05","text":"Probe"}""");

        failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task A_send_to_several_numbers_spends_one_failure_of_the_first_recipient_that_has_any()
    {
        await using var emulator = new EmulatorFactory();
        await ArmFor(emulator, "+46701740606", new { status = 503 });
        await ArmFor(emulator, "+46701740607", new { status = 429 });

        using var failed = await emulator.Send("""{"to":["sms:+46701740605","sms:+46701740606","sms:+46701740607"],"text":"Probe"}""");
        using var next = await emulator.Send(To("+46701740607"));

        failed.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        next.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        Store(emulator).List().Should().BeEmpty();
    }

    [Fact]
    public async Task Failures_for_any_send_are_spent_before_failures_for_a_number()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 502 });
        await ArmFor(emulator, "+46701740605", new { status = 503 });

        using var first = await emulator.Send(Probe);
        using var second = await emulator.Send(Probe);

        first.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        second.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData("/api/v1/send-failures", """[{"status":200}]""", "[0].status must be from 400 to 599.")]
    [InlineData("/api/v1/send-failures", """[{"status":503},{"status":600}]""", "[1].status must be from 400 to 599.")]
    [InlineData("/api/v1/send-failures", """[{"count":0}]""", "[0].count must be 1 or more.")]
    [InlineData("/api/v1/send-failures", """[{"delaySeconds":-1}]""", "[0].delaySeconds must be 0 or more.")]
    [InlineData("/api/v1/send-failures/46701740605", """[{"status":503}]""", "The number must be + followed by digits.")]
    public async Task An_order_outside_the_rules_is_refused_and_arms_nothing(string path, string json, string message)
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PutAsync(new Uri(path, UriKind.Relative), content, TestContext.Current.CancellationToken);
        using var send = await emulator.Send(Probe);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorBody>(TestContext.Current.CancellationToken))!.Message.Should().Be(message);
        send.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static Task Arm(EmulatorFactory emulator, params object[] failures) => Put(emulator, "/api/v1/send-failures", failures);

    private static Task ArmFor(EmulatorFactory emulator, string number, params object[] failures) =>
        Put(emulator, $"/api/v1/send-failures/{number}", failures);

    private static async Task Put(EmulatorFactory emulator, string path, object[] failures)
    {
        using var client = emulator.CreateClient();
        using var response = await client.PutAsJsonAsync(new Uri(path, UriKind.Relative), failures, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static string To(string number) => $$"""{"to":"sms:{{number}}","text":"Probe"}""";

    private static MessageStore Store(EmulatorFactory emulator) => emulator.Services.GetRequiredService<MessageStore>();

    /// <summary>
    /// One second of the emulator's time, then a moment for the send to notice: the delay's timer may start after the
    /// test has moved the clock, so a single jump could pass before it.
    /// </summary>
    private static Task Step(EmulatorFactory emulator)
    {
        emulator.Time.Advance(TimeSpan.FromSeconds(1));

        return Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);
    }

    private sealed record ErrorBody(string Message);
}
