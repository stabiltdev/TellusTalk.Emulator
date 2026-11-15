using Microsoft.AspNetCore.WebUtilities;

namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>
/// A failure a test armed. A 401 takes the form TellusTalk was seen giving; any other status the specification's,
/// plain text with its reason phrase, since K8 has not been seen giving it.
/// </summary>
internal sealed class FailureResult(int statusCode) : TextResult(
    statusCode,
    statusCode == StatusCodes.Status401Unauthorized ? Html : PlainText,
    statusCode == StatusCodes.Status401Unauthorized
        ? TellusTalkUnauthorized.Description
        : ReasonPhrases.GetReasonPhrase(statusCode));
