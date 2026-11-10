namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>One armed failure as the read API serves it; the armed failures go out as a list in the order they are spent.</summary>
internal sealed record SendFailureResponse(int Status, int Remaining, int DelaySeconds)
{
    public static SendFailureResponse From(ArmedFailure failure) => new(failure.Status, failure.Remaining, failure.DelaySeconds);

    public static List<SendFailureResponse> From(IReadOnlyList<ArmedFailure> failures) => [.. failures.Select(From)];
}
