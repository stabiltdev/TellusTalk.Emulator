using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace Stabilt.TellusTalk.Emulator.Tests.Support;

/// <summary>Stands in for an app's report endpoint: every report the emulator posts lands here instead of on the network.</summary>
public sealed class ReportReceiver : HttpMessageHandler
{
    private readonly ConcurrentQueue<ReceivedReport> _received = new();

    public HttpStatusCode Answer { get; set; } = HttpStatusCode.OK;

    /// <summary>Whether a post fails as a refused connection does.</summary>
    public bool Unreachable { get; set; }

    /// <summary>Hosts that take a report and never answer, as a receiver whose packets are dropped does.</summary>
    public ISet<string> SilentHosts { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>When set, every receiver takes its report and answers only once this completes.</summary>
    public TaskCompletionSource? Gate { get; set; }

    public IReadOnlyCollection<ReceivedReport> Received => _received;

    /// <summary>The reports received, waiting a bounded while for <paramref name="count"/> of them.</summary>
    public async Task<IReadOnlyList<ReceivedReport>> WaitFor(int count)
    {
        var clock = Stopwatch.StartNew();
        while (_received.Count < count)
        {
            if (clock.Elapsed > TimeSpan.FromSeconds(5))
            {
                throw new TimeoutException($"{_received.Count} of {count} reports arrived within five seconds.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20), TestContext.Current.CancellationToken);
        }

        return [.. _received];
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (Unreachable)
        {
            throw new HttpRequestException("No connection could be made because the target machine actively refused it.");
        }

        var headers = request.Headers
            .Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
            .ToDictionary(header => header.Key, header => string.Join(", ", header.Value), StringComparer.OrdinalIgnoreCase);
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

        _received.Enqueue(new ReceivedReport(request.Method, request.RequestUri!, headers, body));

        if (SilentHosts.Contains(request.RequestUri!.Host))
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (Gate is { } gate)
        {
            await gate.Task.WaitAsync(cancellationToken);
        }

        return new HttpResponseMessage(Answer);
    }
}

public sealed record ReceivedReport(HttpMethod Method, Uri Uri, IReadOnlyDictionary<string, string> Headers, string Body);
