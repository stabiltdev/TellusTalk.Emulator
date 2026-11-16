using System.Text.Json;
using System.Text.Json.Serialization;
using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>
/// TellusTalk's answer to a send it takes: <c>application/json</c> with no charset, the job's id three times and the
/// recipient count. <see cref="TypedResults.Ok{TValue}"/> would add a charset, so the answer is written as text.
/// </summary>
internal sealed class SendAccepted(SendJob job) :
    IResult,
    IStatusCodeHttpResult,
    IContentTypeHttpResult,
    IValueHttpResult,
    IValueHttpResult<SendAnswer>
{
    public SendAnswer Value { get; } = new(job.JobId, job.JobId, job.JobId, job.Size);

    public int StatusCode { get; } = StatusCodes.Status200OK;

    public string ContentType { get; } = "application/json";

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    object? IValueHttpResult.Value => Value;

    public Task ExecuteAsync(HttpContext httpContext) =>
        TypedResults.Text(JsonSerializer.Serialize(Value), ContentType, statusCode: StatusCode).ExecuteAsync(httpContext);
}

internal sealed record SendAnswer(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("job_uuid")] string JobUuid,
    [property: JsonPropertyName("log_uuid")] string LogUuid,
    [property: JsonPropertyName("job_size")] int JobSize);
