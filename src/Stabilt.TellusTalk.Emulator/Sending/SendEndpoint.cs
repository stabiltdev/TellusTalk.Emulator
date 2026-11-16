using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Stabilt.TellusTalk.Emulator.Http;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Requests;
using Stabilt.TellusTalk.Emulator.Sending.HttpResults;
using Accepted = Stabilt.TellusTalk.Emulator.Requests.Accepted;

namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>TellusTalk's own K8 send API, at the path TellusTalk serves it.</summary>
internal sealed class SendEndpoint : IEndpoint
{
    private const string NotJson =
        "TellusTalk Emulator: the emulator takes JSON bodies only. TellusTalk itself also takes XML and form bodies.";

    // The headers' filter comes first, so the answer to a failure carries the headers too.
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder
        .MapPost("send/v1", Handle)
        .RequireAuthorization()
        .AddEndpointFilter<TellusTalkHeadersFilter>()
        .AddEndpointFilter<AnySendFailureFilter>();

    /// <summary>
    /// Reads the body itself rather than binding it, so that every refusal is TellusTalk's or names the emulator's
    /// limit, and so the body is kept exactly as it arrived. Bound as a stream, the body is still unread when the
    /// filters run.
    /// </summary>
    private static async Task<Results<
        SendAccepted,
        TellusTalkRefusal,
        EmulatorLimit,
        FailureResult,
        EmptyHttpResult
    >> Handle(
        [FromHeader(Name = "Content-Type")] string? contentType,
        Stream body,
        ClaimsPrincipal user,
        TimeProvider time,
        MessageFactory factory,
        MessageStore store,
        SendFailures failures,
        CancellationToken cancellationToken)
    {
        if (!IsJson(contentType))
        {
            return Refusals.EmulatorLimit(StatusCodes.Status415UnsupportedMediaType, NotJson);
        }

        using var reader = new StreamReader(body, Encoding.UTF8);
        var rawRequest = await reader.ReadToEndAsync(cancellationToken);

        var read = SendRequestReader.Read(rawRequest);
        if (read is not Accepted { Request: var request })
        {
            return read switch
            {
                Refused refused => Refusals.EmulatorLimit(StatusCodes.Status400BadRequest, refused.Text),
                RefusedByTellusTalk refusedByTellusTalk => Refusals.TellusTalk(refusedByTellusTalk.Text),
                _ => throw new InvalidOperationException($"The read result '{read}' has no answer."),
            };
        }

        // A number's failures need the recipients, so they are spent once the request is read. An e-mail address or a
        // group's id is no number, whatever it reads as.
        var numbers = request.Recipients
            .Where(recipient => recipient.Channel
                is MessageChannel.Sms
                or MessageChannel.Mms
                or MessageChannel.Voice
                or MessageChannel.Fax)
            .Select(recipient => recipient.Address);

        if (failures.TrySpend(numbers) is { } numberFailure)
        {
            return await numberFailure.WaitOutDelay(time, cancellationToken)
                ? Refusals.Failure(numberFailure.Status)
                : TypedResults.Empty;
        }

        var job = factory.Create(request, user.Identity!.Name!, rawRequest);
        store.Add(job.Messages);

        return new SendAccepted(job);
    }

    private static bool IsJson(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var mediaType) &&
        string.Equals(mediaType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);
}
