using System.Net;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Api;

public sealed class HealthTests
{
    [Fact]
    public async Task The_health_endpoint_answers_healthy()
    {
        await using var emulator = new EmulatorFactory();
        using var client = emulator.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/health", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/plain");
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be("Healthy");
    }
}
