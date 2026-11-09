using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal sealed class SetSendFailuresEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapPut("api/v1/send-failures", Handle);

    /// <summary>Only a 5xx: the specification's temporary error, the one a client is told to retry.</summary>
    private static Results<Ok<SendFailuresResponse>, BadRequest<ApiError>> Handle(SendFailures failures, FailureOrder order)
    {
        var status = order.Status ?? StatusCodes.Status500InternalServerError;
        if (status is < 500 or > 599)
        {
            return TypedResults.BadRequest(new ApiError("status must be from 500 to 599."));
        }

        var count = order.Count ?? 1;
        if (count < 1)
        {
            return TypedResults.BadRequest(new ApiError("count must be 1 or more."));
        }

        return TypedResults.Ok(SendFailuresResponse.From(failures.Arm(status, count)));
    }

    private sealed record FailureOrder(int? Status, int? Count);
}
