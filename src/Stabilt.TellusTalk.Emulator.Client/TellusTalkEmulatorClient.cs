using System.Net;
using System.Net.Http.Json;

namespace Stabilt.TellusTalk.Emulator.Client;

/// <summary>Reads what TellusTalk Emulator caught, through the emulator's own API.</summary>
public sealed class TellusTalkEmulatorClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    /// <summary>Reads through <paramref name="httpClient"/>, whose base address is the emulator's, and never disposes it.</summary>
    public TellusTalkEmulatorClient(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        if (httpClient.BaseAddress is null)
        {
            throw new ArgumentException("The client needs the emulator's address as the HttpClient's BaseAddress.", nameof(httpClient));
        }

        _http = httpClient;
    }

    /// <summary>Reads from the emulator at <paramref name="baseAddress"/> through an HttpClient it disposes with itself.</summary>
    public TellusTalkEmulatorClient(Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(baseAddress);

        _http = new HttpClient { BaseAddress = baseAddress };
        _ownsHttp = true;
    }

    /// <summary>The message with <paramref name="id"/>, or <c>null</c> when the emulator has none.</summary>
    public async Task<CaughtMessage?> GetMessageAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        using var response = await _http.GetAsync(new Uri($"api/messages/{Uri.EscapeDataString(id)}", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CaughtMessage>(ClientJson.Options, cancellationToken);
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }
}
