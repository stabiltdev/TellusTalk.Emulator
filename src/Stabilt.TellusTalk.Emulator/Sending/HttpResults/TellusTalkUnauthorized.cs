namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>
/// TellusTalk's answer to a send without credentials, word for word: Werkzeug's standard description of a 401,
/// served as HTML though it holds no markup, and with no challenge.
/// </summary>
internal sealed class TellusTalkUnauthorized() : TextResult(StatusCodes.Status401Unauthorized, Html, Description)
{
    public const string Description =
        "The server could not verify that you are authorized to access the URL requested. You either supplied the wrong " +
        "credentials (e.g. a bad password), or your browser doesn't understand how to supply the credentials required.";
}
