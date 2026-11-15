using System.Net;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Client.Tests;

public sealed class TellusTalkEmulatorClientTests
{
    [Fact]
    public void A_client_needs_an_HttpClient_with_a_base_address()
    {
        using var http = new HttpClient();

        var create = () => new TellusTalkEmulatorClient(http);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task A_client_given_an_HttpClient_leaves_it_usable_after_disposal()
    {
        await using var emulator = new EmulatorFactory();
        using var http = emulator.CreateClient();

        new TellusTalkEmulatorClient(http).Dispose();

        using var response = await http.GetAsync(new Uri("api/health", UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public Task A_client_made_from_an_address_disposes_its_own_HttpClient()
    {
        var client = new TellusTalkEmulatorClient(new Uri("http://127.0.0.1:9/"));

        client.Dispose();

        var read = () => client.GetMessageAsync("any", TestContext.Current.CancellationToken);
        return read.Should().ThrowAsync<ObjectDisposedException>();
    }
}
