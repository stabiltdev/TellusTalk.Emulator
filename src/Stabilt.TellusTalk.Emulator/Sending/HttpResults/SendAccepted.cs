using System.Text.Json;
using System.Text.Json.Serialization;
using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>
/// TellusTalk's answer to a send it takes, as <c>application/json</c> with no charset.
/// <see cref="TypedResults.Ok{TValue}"/> would add a charset, so the response is written as text.
/// </summary>
internal sealed class SendAccepted(SendResponse response) :
    IResult,
    IStatusCodeHttpResult,
    IContentTypeHttpResult,
    IValueHttpResult,
    IValueHttpResult<SendResponse>
{
    public SendResponse Value { get; } = response;

    public int StatusCode { get; } = StatusCodes.Status200OK;

    public string ContentType { get; } = "application/json";

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    object? IValueHttpResult.Value => Value;

    public Task ExecuteAsync(HttpContext httpContext) =>
        TypedResults.Text(JsonSerializer.Serialize(Value), ContentType, statusCode: StatusCode).ExecuteAsync(httpContext);
}

internal sealed record SendResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("job_uuid")] string JobUuid,
    [property: JsonPropertyName("log_uuid")] string LogUuid,
    [property: JsonPropertyName("job_size")] int JobSize)
{
    /// <summary>K8 gives the job's id as all three ids, and the number of recipients as the size.</summary>
    public static SendResponse From(SendJob job) => new(job.JobId, job.JobId, job.JobId, job.Size);
}
