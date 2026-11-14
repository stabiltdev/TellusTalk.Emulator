using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal static class Refusals
{
    /// <summary>
    /// TellusTalk's answer to a send without credentials, word for word: Werkzeug's standard description of a 401,
    /// served as HTML though it holds no markup, and with no challenge.
    /// </summary>
    private const string UnauthorizedText =
        "The server could not verify that you are authorized to access the URL requested. You either supplied the wrong " +
        "credentials (e.g. a bad password), or your browser doesn't understand how to supply the credentials required.";

    public static Task Unauthorized(HttpResponse response) =>
        Write(response, StatusCodes.Status401Unauthorized, "text/html; charset=utf-8", UnauthorizedText);

    /// <summary>
    /// A failure a test armed. A 401 is TellusTalk's own, as observed; any other status takes the specification's form,
    /// plain text with its reason phrase, since K8 has not been seen giving it.
    /// </summary>
    public static Task Failure(HttpResponse response, int statusCode) =>
        statusCode == StatusCodes.Status401Unauthorized
            ? Unauthorized(response)
            : Write(response, statusCode, "text/plain; charset=utf-8", ReasonPhrases.GetReasonPhrase(statusCode));

    /// <summary>A request K8 was seen to refuse: its text as the body, served as HTML though it holds no markup.</summary>
    public static Task TellusTalk(HttpResponse response, string text) =>
        Write(response, StatusCodes.Status400BadRequest, "text/html; charset=utf-8", text);

    /// <summary>A request the emulator cannot record; <paramref name="text"/> says it is the emulator's limit.</summary>
    public static Task EmulatorLimit(HttpResponse response, int statusCode, string text) =>
        Write(response, statusCode, "text/plain; charset=utf-8", text);

    private static Task Write(HttpResponse response, int statusCode, string contentType, string text)
    {
        response.StatusCode = statusCode;
        response.ContentType = contentType;
        response.ContentLength = Encoding.UTF8.GetByteCount(text);

        return response.WriteAsync(text);
    }
}
