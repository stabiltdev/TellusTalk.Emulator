using System.Net;
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
    public async Task Armed_failures_answer_the_next_sends_with_their_status_and_then_let_sends_through()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 503, count = 2 });

        using var first = await emulator.Send(Probe);
        using var second = await emulator.Send(Probe);
        using var third = await emulator.Send(Probe);

        first.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        second.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        third.StatusCode.Should().Be(HttpStatusCode.OK);
        emulator.Services.GetRequiredService<MessageStore>().List().Should().ContainSingle();
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
    public async Task A_send_without_credentials_is_refused_before_a_failure_is_spent()
    {
        await using var emulator = new EmulatorFactory();
        await Arm(emulator, new { status = 502 });

        using var anonymous = await emulator.Send(Probe, user: null);
        using var authenticated = await emulator.Send(Probe);

        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        authenticated.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Theory]
    [InlineData("""{"status":404}""", "status must be from 500 to 599.")]
    [InlineData("""{"status":600}""", "status must be from 500 to 599.")]
    [InlineData("""{"count":0}""", "count must be 1 or more.")]
    public async Task A_status_outside_5xx_or_a_count_below_one_is_refused(string json, string message)
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PutAsync(new Uri("/api/v1/send-failures", UriKind.Relative), content, TestContext.Current.CancellationToken);
        using var send = await emulator.Send(Probe);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<ErrorBody>(TestContext.Current.CancellationToken))!.Message.Should().Be(message);
        send.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task Arm(EmulatorFactory emulator, object order)
    {
        using var client = emulator.CreateClient();
        using var response = await client.PutAsJsonAsync(new Uri("/api/v1/send-failures", UriKind.Relative), order, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed record ErrorBody(string Message);
}
