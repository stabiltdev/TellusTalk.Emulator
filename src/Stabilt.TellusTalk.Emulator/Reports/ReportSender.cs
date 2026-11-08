using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Stabilt.TellusTalk.Emulator.Ids;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Reports;

/// <summary>
/// Posts a message's delivery report to each of its report addresses that is a URL. The emulator sends no e-mail and no
/// SMS, so an e-mail or SMS report address is recorded as not sent.
/// </summary>
internal sealed class ReportSender(IHttpClientFactory clients, TimeProvider time)
{
    public const string ClientName = "reports";

    private const string JsonMediaType = "application/json";

    public async Task<IReadOnlyList<ReportAttempt>> Send(Message message, ReportStatus status, CancellationToken cancellationToken) =>
        await Task.WhenAll(message.Request.ReportTo.Select(address => SendTo(address, message, status, cancellationToken)));

    private async Task<ReportAttempt> SendTo(ReportAddress address, Message message, ReportStatus status, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();

        var reason = address switch
        {
            { Kind: ReportAddressKind.Email } => "Not sent: the emulator sends no e-mail.",
            { Kind: ReportAddressKind.Sms } => "Not sent: the emulator sends no SMS.",
            { ContentType: { } type } when !string.Equals(type, JsonMediaType, StringComparison.OrdinalIgnoreCase) =>
                $"Not sent: TellusTalk's {type} report is unknown; the emulator writes JSON only.",
            _ => null,
        };

        if (reason is not null)
        {
            return new ReportAttempt(now, status, address.Target, Sent: false, reason);
        }

        using var request = Request(address, DeliveryReport.Json(DeliveryReport.For(message, status, now)));
        try
        {
            using var response = await clients
                .CreateClient(ClientName)
                .SendAsync(request, cancellationToken);

            return new ReportAttempt(
                now,
                status,
                address.Target,
                Sent: true,
                string.Create(CultureInfo.InvariantCulture, $"{(int)response.StatusCode} {response.ReasonPhrase}"));
        }
        catch (HttpRequestException exception)
        {
            return new ReportAttempt(now, status, address.Target, Sent: false, $"Not delivered: {exception.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ReportAttempt(now, status, address.Target, Sent: false, "Not delivered: the receiver did not answer in time.");
        }
    }

    /// <summary>
    /// The headers TellusTalk was seen sending a report with, the request id in its UUID form, and any header the
    /// address's settings add. The user agent names the emulator rather than copying TellusTalk's.
    /// </summary>
    private HttpRequestMessage Request(ReportAddress address, string body)
    {
        var content = new StringContent(body, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue(JsonMediaType);

        var request = new HttpRequestMessage(HttpMethod.Post, address.Target) { Content = content };
        var requestId = Guid.CreateVersion1(time.GetUtcNow()).ToString("N");
        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.UserAgent.ParseAdd("Stabilt.TellusTalk.Emulator");
        request.Headers.TryAddWithoutValidation("webpages-request-id", requestId);
        request.Headers.TryAddWithoutValidation("x-webpages-request-id", requestId);

        foreach (var (name, value) in address.Headers)
        {
            if (!request.Headers.TryAddWithoutValidation(name, value))
            {
                content.Headers.TryAddWithoutValidation(name, value);
            }
        }

        return request;
    }
}
