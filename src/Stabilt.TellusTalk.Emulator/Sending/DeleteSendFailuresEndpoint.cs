using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal sealed class DeleteSendFailuresEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapDelete("api/v1/send-failures/{number?}", Handle);

    /// <summary>
    /// Without a number, every failure goes, every number's too, so a test suite can start over with one call; a test
    /// that keeps to its own number clears only that.
    /// </summary>
    private static Results<NoContent, BadRequest<ApiError>> Handle(SendFailures failures, string? number)
    {
        if (FailureNumber.Refusal(number) is { } refusal)
        {
            return refusal;
        }

        if (number is null)
        {
            failures.Clear();
        }
        else
        {
            failures.Clear(number);
        }

        return TypedResults.NoContent();
    }
}
