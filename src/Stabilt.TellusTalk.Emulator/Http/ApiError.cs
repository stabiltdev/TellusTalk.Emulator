namespace Stabilt.TellusTalk.Emulator.Http;

/// <summary>The body of the read API's <c>400</c> and <c>415</c>.</summary>
internal sealed record ApiError(string Message);
