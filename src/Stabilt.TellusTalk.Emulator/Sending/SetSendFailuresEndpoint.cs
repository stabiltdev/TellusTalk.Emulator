using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal sealed class SetSendFailuresEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapPut("api/v1/send-failures", Handle);

    private static Results<Ok<SendFailuresResponse>, BadRequest<ApiError>> Handle(SendFailures failures, FailureOrder? order)
    {
        var status = order?.Status ?? StatusCodes.Status500InternalServerError;
        if (status is < 400 or > 599)
        {
            return TypedResults.BadRequest(new ApiError("status must be from 400 to 599."));
        }

        var count = order?.Count ?? 1;
        if (count < 1)
        {
            return TypedResults.BadRequest(new ApiError("count must be 1 or more."));
        }

        var delaySeconds = order?.DelaySeconds ?? 0;
        if (delaySeconds < 0)
        {
            return TypedResults.BadRequest(new ApiError("delaySeconds must be 0 or more."));
        }

        return TypedResults.Ok(SendFailuresResponse.From(failures.Arm(status, count, delaySeconds)));
    }

    private sealed record FailureOrder(int? Status, int? Count, int? DelaySeconds);
}
