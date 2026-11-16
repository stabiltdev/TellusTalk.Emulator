namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>
/// An answer to a send whose body is a text. Each kind is a type of its own, so the handler's <c>Results</c> names
/// every answer it gives; <see cref="Refusals"/> makes them.
/// </summary>
internal abstract class TextResult(int statusCode, string contentType, string text) :
    IResult,
    IStatusCodeHttpResult,
    IContentTypeHttpResult,
    IValueHttpResult,
    IValueHttpResult<string>
{
    protected const string Html = "text/html; charset=utf-8";
    protected const string PlainText = "text/plain; charset=utf-8";

    public int StatusCode { get; } = statusCode;

    public string ContentType { get; } = contentType;

    public string Value { get; } = text;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    object? IValueHttpResult.Value => Value;

    public Task ExecuteAsync(HttpContext httpContext) =>
        TypedResults.Text(Value, ContentType, statusCode: StatusCode).ExecuteAsync(httpContext);
}
