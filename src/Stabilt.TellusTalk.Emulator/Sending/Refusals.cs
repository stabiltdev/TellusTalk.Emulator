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
    /// A failure a test armed, in the specification's form for a 5xx: plain text with the status's reason phrase. K8 has
    /// not been seen failing.
    /// </summary>
    public static Task Failure(HttpResponse response, int statusCode) =>
        Write(response, statusCode, "text/plain; charset=utf-8", ReasonPhrases.GetReasonPhrase(statusCode));

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
