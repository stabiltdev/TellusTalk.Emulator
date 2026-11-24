using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Requests;

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
    /// <summary>
    /// K8 writes the <c>+</c> of a number as it is, which the default encoder escapes as HTML-sensitive; a JSON
    /// answer is never read as HTML.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public SendResponse Value { get; } = response;

    public int StatusCode { get; } = StatusCodes.Status200OK;

    public string ContentType { get; } = "application/json";

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    object? IValueHttpResult.Value => Value;

    public Task ExecuteAsync(HttpContext httpContext) =>
        TypedResults
            .Text(JsonSerializer.Serialize(Value, Options), ContentType, statusCode: StatusCode)
            .ExecuteAsync(httpContext);
}

/// <summary>K8's members in K8's order, each optional one written only when it has something.</summary>
internal sealed record SendResponse(
    [property: JsonPropertyName("job_id")] string JobId,
    [property: JsonPropertyName("job_uuid")] string JobUuid,
    [property: JsonPropertyName("invalid_report_to"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<InvalidAddress>? InvalidReportTo,
    [property: JsonPropertyName("invalid_reply_to"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<InvalidAddress>? InvalidReplyTo,
    [property: JsonPropertyName("log_uuid"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? LogUuid,
    [property: JsonPropertyName("invalid_recipients"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<InvalidAddress>? InvalidRecipients,
    [property: JsonPropertyName("job_size")] int JobSize)
{
    private const string UnknownAddressType = "Unknown Address Type";

    /// <summary>
    /// K8 gives the job's id as each of its ids, and the number of recipients as the size, blocked ones included. With
    /// several recipients each message has a log id of its own, and K8 answers with none.
    /// </summary>
    public static SendResponse From(SendJob job, SendRequest request) => new(
        job.JobId,
        job.JobId,
        Invalid(request.InvalidReportTo, UnknownAddressType),
        Invalid(request.InvalidReplyTo, UnknownAddressType),
        job.Size == 1 ? job.JobId : null,
        Blocked(request.Recipients),
        job.Size);

    private static List<InvalidAddress>? Invalid(IReadOnlyList<string> addresses, string error) =>
        addresses.Count == 0
            ? null
            : [.. addresses.Select(address => new InvalidAddress(address, error))];

    private static List<InvalidAddress>? Blocked(IReadOnlyList<Recipient> recipients)
    {
        List<InvalidAddress> invalid =
        [
            .. recipients
                .Where(recipient => recipient.Invalid is not null)
                .Select(recipient => new InvalidAddress(recipient.AsSent, Error(recipient.Invalid!.Value))),
        ];

        return invalid.Count == 0 ? null : invalid;
    }

    private static string Error(InvalidNumber invalid) => invalid switch
    {
        InvalidNumber.Missing => "Missing Address",
        InvalidNumber.NotANumber => "Not a Number",
        InvalidNumber.NotNumeric => "Number Not Numeric",
        _ => throw new InvalidOperationException($"No error for the invalid number '{invalid}'."),
    };
}

internal sealed record InvalidAddress(
    [property: JsonPropertyName("address")] string Address,
    [property: JsonPropertyName("error")] string Error);
