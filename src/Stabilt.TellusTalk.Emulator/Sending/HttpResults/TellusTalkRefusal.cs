namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

/// <summary>A request K8 was seen to refuse: its text as the body, served as HTML though it holds no markup.</summary>
internal sealed class TellusTalkRefusal(string text) : TextResult(StatusCodes.Status400BadRequest, Html, text);
