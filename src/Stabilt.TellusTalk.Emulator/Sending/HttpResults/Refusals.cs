namespace Stabilt.TellusTalk.Emulator.Sending.HttpResults;

internal static class Refusals
{
    public static TellusTalkUnauthorized Unauthorized() => new();

    public static FailureResult Failure(int statusCode) => new(statusCode);

    public static TellusTalkRefusal TellusTalk(string text) => new(text);

    public static EmulatorLimit EmulatorLimit(int statusCode, string text) => new(statusCode, text);
}
