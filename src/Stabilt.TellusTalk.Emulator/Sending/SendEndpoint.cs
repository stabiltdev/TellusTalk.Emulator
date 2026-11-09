using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stabilt.TellusTalk.Emulator.Http;
using Stabilt.TellusTalk.Emulator.Ids;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>TellusTalk's own K8 send API, at the path TellusTalk serves it.</summary>
internal sealed class SendEndpoint : IEndpoint
{
    private const string NotJson =
        "TellusTalk Emulator: the emulator takes JSON bodies only. TellusTalk itself also takes XML and form bodies.";

    private const string InvalidJson = "TellusTalk Emulator: the request body is not valid JSON.";

    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder
        .MapPost("send/v1", Handle)
        .RequireAuthorization();

    /// <summary>
    /// Reads the body itself rather than binding it, so that every refusal is TellusTalk's or names the emulator's
    /// limit, and so the body is kept exactly as it arrived.
    /// </summary>
    private static async Task Handle(
        HttpContext context,
        ClaimsPrincipal user,
        TimeProvider time,
        MessageFactory factory,
        MessageStore store,
        SendFailures failures)
    {
        var response = context.Response;
        TellusTalkHeaders.Apply(response, Guid.CreateVersion1(time.GetUtcNow()).ToString("N"));

        // Before the body is read: a failing TellusTalk takes nothing, whatever the request holds.
        if (failures.TrySpend() is { } failure)
        {
            try
            {
                await Task.Delay(failure.Delay, time, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // The client gave up waiting, which is what a delay is armed to make it do.
                return;
            }

            await Refusals.Failure(response, failure.Status);

            return;
        }

        if (!IsJson(context.Request.ContentType))
        {
            await Refusals.EmulatorLimit(response, StatusCodes.Status415UnsupportedMediaType, NotJson);

            return;
        }

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(context.RequestAborted);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            await Refusals.EmulatorLimit(response, StatusCodes.Status400BadRequest, InvalidJson);

            return;
        }

        using (document)
        {
            switch (SendRequestReader.Read(document.RootElement))
            {
                case Refused refused:
                    await Refusals.EmulatorLimit(response, StatusCodes.Status400BadRequest, refused.Text);
                    break;

                case Accepted accepted:
                    var job = factory.Create(accepted.Request, user.Identity!.Name!, body);
                    store.Add(job.Messages);
                    await Answer(response, job);
                    break;
            }
        }
    }

    private static bool IsJson(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var mediaType) &&
        string.Equals(mediaType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase);

    /// <summary>TellusTalk answers with <c>application/json</c> and no charset, the job's id three times and the recipient count.</summary>
    private static Task Answer(HttpResponse response, SendJob job)
    {
        var answer = JsonSerializer.Serialize(new SendAnswer(job.JobId, job.JobId, job.JobId, job.Size));
        response.ContentType = "application/json";
        response.ContentLength = Encoding.UTF8.GetByteCount(answer);

        return response.WriteAsync(answer);
    }

    private sealed record SendAnswer(
        [property: JsonPropertyName("job_id")] string JobId,
        [property: JsonPropertyName("job_uuid")] string JobUuid,
        [property: JsonPropertyName("log_uuid")] string LogUuid,
        [property: JsonPropertyName("job_size")] int JobSize);
}
