using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Sending;

public sealed class SendEndpointTests
{
    private const string Uuid1 = "^[0-9a-f]{12}1[0-9a-f]{3}[89ab][0-9a-f]{15}$";

    private const string Probe = """{"to":"sms:+46701740605","text":"Probe 1"}""";

    private const string UnauthorizedText =
        "The server could not verify that you are authorized to access the URL requested. You either supplied the wrong " +
        "credentials (e.g. a bad password), or your browser doesn't understand how to supply the credentials required.";

    [Fact]
    public async Task A_send_without_credentials_is_refused_with_TellusTalks_text_and_no_challenge()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send(Probe, user: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.ToString().Should().Be("text/html; charset=utf-8");
        (await Body(response)).Should().Be(UnauthorizedText);
        response.Headers.WwwAuthenticate.Should().BeEmpty();
        response.Headers.Contains("X-Request-Id").Should().BeTrue();
    }

    [Fact]
    public async Task Credentials_without_a_colon_are_refused()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await SendWithAuthorization(emulator, "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("no-colon")));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("basic")]
    [InlineData("BASIC")]
    public async Task The_authorization_scheme_is_read_in_any_case(string scheme)
    {
        await using var emulator = new EmulatorFactory();

        using var response = await SendWithAuthorization(emulator, scheme, Convert.ToBase64String(Encoding.UTF8.GetBytes("tester:secret")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_body_that_is_not_JSON_is_refused_as_the_emulators_limit()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send(Probe, contentType: "text/plain");

        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        response.Content.Headers.ContentType!.ToString().Should().Be("text/plain; charset=utf-8");
        (await Body(response)).Should().StartWith("TellusTalk Emulator:");
    }

    [Fact]
    public async Task A_JSON_content_type_with_a_charset_is_accepted()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send(Probe, contentType: "application/json; charset=utf-8");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JSON_that_does_not_parse_is_refused_as_the_emulators_limit()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send("{");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Body(response)).Should().StartWith("TellusTalk Emulator:");
    }

    [Fact]
    public async Task A_recipient_on_another_channel_is_refused_with_the_readers_text()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send("""{"to":"email:a@b.se","text":"x"}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Body(response)).Should().Be("TellusTalk Emulator: the emulator handles sms: recipients only, not 'email:'. TellusTalk itself accepts them.");
    }

    [Fact]
    public async Task A_send_is_answered_as_TellusTalk_answers_one()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send(Probe);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.ToString().Should().Be("application/json");

        using var answer = JsonDocument.Parse(await Body(response));
        var root = answer.RootElement;
        root.EnumerateObject()
            .Select(property => property.Name)
            .Should().Equal("job_id", "job_uuid", "log_uuid", "job_size");
        var jobId = root.GetProperty("job_id").GetString();
        jobId.Should().MatchRegex(Uuid1);
        root.GetProperty("job_uuid").GetString().Should().Be(jobId);
        root.GetProperty("log_uuid").GetString().Should().Be(jobId);
        root.GetProperty("job_size").ValueKind.Should().Be(JsonValueKind.Number);
        root.GetProperty("job_size").GetInt32().Should().Be(1);

        var requestId = response.Headers
            .GetValues("X-Request-Id")
            .Single();
        requestId.Should().MatchRegex(Uuid1).And.NotBe(jobId);
        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "Strict-Transport-Security").Should().Be("max-age=31536000; includeSubDomains");
        Header(response, "Cross-Origin-Opener-Policy").Should().Be("same-origin");
        Header(response, "Referrer-Policy").Should().Be("strict-origin-when-cross-origin");
        Header(response, "Cross-Origin-Resource-Policy").Should().Be("same-origin");
        Header(response, "Permissions-Policy").Should().Be("geolocation=(), camera=(), microphone=()");
        response.Headers.Server.Should().BeEmpty();

        Store(emulator).List().Should().ContainSingle().Which.UserName.Should().Be("tester");
    }

    [Fact]
    public async Task An_unset_DateTime_as_scheduled_time_is_sent_at_once()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send("""{"to":"sms:+46701740605","text":"Hej","scheduled_send_time":"0001-01-01T00:00:00"}""");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Store(emulator).List(MessageState.Sent).Should().ContainSingle();
    }

    [Fact]
    public async Task A_send_to_two_numbers_stores_two_messages()
    {
        await using var emulator = new EmulatorFactory();

        using var response = await emulator.Send("""{"to":["sms:+46701740605","sms:+46701740606"],"text":"Hej"}""");

        using var answer = JsonDocument.Parse(await Body(response));
        answer.RootElement.GetProperty("job_size").GetInt32().Should().Be(2);
        Store(emulator).List().Should().HaveCount(2);
    }

    [Fact]
    public async Task A_send_to_more_recipients_than_the_retention_limit_answers_for_all_and_keeps_the_newest()
    {
        await using var emulator = new EmulatorFactory();
        emulator.Settings["Emulator:RetentionLimit"] = "3";

        using var response = await emulator.Send(
            """{"to":["sms:+46701740601","sms:+46701740602","sms:+46701740603","sms:+46701740604","sms:+46701740605"],"text":"Hej"}""");

        using var answer = JsonDocument.Parse(await Body(response));
        answer.RootElement.GetProperty("job_size").GetInt32().Should().Be(5);
        Store(emulator).List()
            .Select(message => message.To.Number)
            .Should().BeEquivalentTo(["+46701740603", "+46701740604", "+46701740605"]);
    }

    [Fact]
    public async Task Parallel_sends_each_store_a_message_of_their_own()
    {
        await using var emulator = new EmulatorFactory();

        var responses = await Task.WhenAll(Enumerable
            .Range(0, 50)
            .Select(_ => emulator.Send(Probe)));

        responses.Should().AllSatisfy(response => response.StatusCode.Should().Be(HttpStatusCode.OK));
        Store(emulator).List()
            .Select(message => message.Id)
            .Should().HaveCount(50).And.OnlyHaveUniqueItems();

        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    private static async Task<HttpResponseMessage> SendWithAuthorization(EmulatorFactory emulator, string scheme, string parameter)
    {
        using var client = emulator.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/send/v1", UriKind.Relative))
        {
            Content = new StringContent(Probe, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static MessageStore Store(EmulatorFactory emulator) => emulator.Services.GetRequiredService<MessageStore>();

    private static Task<string> Body(HttpResponseMessage response) =>
        response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    private static string Header(HttpResponseMessage response, string name) => response.Headers.GetValues(name).Single();
}
