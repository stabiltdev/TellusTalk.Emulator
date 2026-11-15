using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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

    /// <summary>The messages the emulator caught, newest first, narrowed by <paramref name="query"/>'s filters.</summary>
    public async Task<IReadOnlyList<CaughtMessage>> GetMessagesAsync(MessageQuery? query = null, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync(MessagesUri(query), cancellationToken);
        response.EnsureSuccessStatusCode();

        var list = await response.Content.ReadFromJsonAsync<MessageList>(ClientJson.Options, cancellationToken);

        return list!.Messages;
    }

    /// <summary>Deletes every message, the held-back ones too; send failures a test armed stay.</summary>
    public async Task DeleteMessagesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync(new Uri("api/messages", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>The message with <paramref name="id"/>, or <c>null</c> when the emulator has none.</summary>
    public async Task<CaughtMessage?> GetMessageAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        if (id is "." or "..")
        {
            throw new ArgumentException("The id cannot be . or .., which a URL resolves as a step in the path rather than as an id.", nameof(id));
        }

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

    /// <summary>
    /// Escapes each value, since the query decodes a <c>+</c> as a space and ends a value at <c>&amp;</c> or <c>#</c>. An
    /// enum goes as its camelCase name, as the read API reads it.
    /// </summary>
    private static Uri MessagesUri(MessageQuery? query)
    {
        (string Name, string? Value)[] filters =
        [
            ("state", query?.State is { } state ? JsonNamingPolicy.CamelCase.ConvertName(state.ToString()) : null),
            ("channel", query?.Channel is { } channel ? JsonNamingPolicy.CamelCase.ConvertName(channel.ToString()) : null),
            ("to", query?.To),
            ("q", query?.Search),
        ];

        var given = filters
            .Where(filter => filter.Value is not null)
            .Select(filter => $"{filter.Name}={Uri.EscapeDataString(filter.Value!)}")
            .ToList();

        return new Uri(given.Count == 0 ? "api/messages" : $"api/messages?{string.Join('&', given)}", UriKind.Relative);
    }
}
