using Microsoft.AspNetCore.WebUtilities;

namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>
/// An answer to a send whose body is a text. Each kind is a type of its own, so the handler's <c>Results</c> names
/// every answer it gives; <see cref="Refusals"/> makes them.
/// </summary>
internal abstract class TextAnswer(int statusCode, string contentType, string text) :
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

/// <summary>
/// TellusTalk's answer to a send without credentials, word for word: Werkzeug's standard description of a 401,
/// served as HTML though it holds no markup, and with no challenge.
/// </summary>
internal sealed class TellusTalkUnauthorized() : TextAnswer(StatusCodes.Status401Unauthorized, Html, Description)
{
    public const string Description =
        "The server could not verify that you are authorized to access the URL requested. You either supplied the wrong " +
        "credentials (e.g. a bad password), or your browser doesn't understand how to supply the credentials required.";
}

/// <summary>A request K8 was seen to refuse: its text as the body, served as HTML though it holds no markup.</summary>
internal sealed class TellusTalkRefusal(string text) : TextAnswer(StatusCodes.Status400BadRequest, Html, text);

/// <summary>A request the emulator cannot record; its text says it is the emulator's limit.</summary>
internal sealed class EmulatorLimit(int statusCode, string text) : TextAnswer(statusCode, PlainText, text);

/// <summary>
/// A failure a test armed. A 401 takes the form TellusTalk was seen giving; any other status the specification's,
/// plain text with its reason phrase, since K8 has not been seen giving it.
/// </summary>
internal sealed class FailureAnswer(int statusCode) : TextAnswer(
    statusCode,
    statusCode == StatusCodes.Status401Unauthorized ? Html : PlainText,
    statusCode == StatusCodes.Status401Unauthorized
        ? TellusTalkUnauthorized.Description
        : ReasonPhrases.GetReasonPhrase(statusCode));
