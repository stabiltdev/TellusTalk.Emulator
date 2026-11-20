namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>A request K8 was seen to answer with a server error: its text as the body, served as HTML as its refusals are.</summary>
internal sealed class TellusTalkError(string text) : TextResult(StatusCodes.Status500InternalServerError, Html, text);
