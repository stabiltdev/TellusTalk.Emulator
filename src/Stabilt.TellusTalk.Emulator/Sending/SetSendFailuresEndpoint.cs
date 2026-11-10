using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal sealed class SetSendFailuresEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapPut("api/v1/send-failures/{number?}", Handle);

    /// <summary>Without a number the failures are for any send; without a body, one send fails with 500.</summary>
    private static Results<Ok<List<SendFailureResponse>>, BadRequest<ApiError>> Handle(
        SendFailures failures,
        string? number,
        List<FailureOrder?>? orders)
    {
        if (FailureNumber.Refusal(number) is { } refusal)
        {
            return refusal;
        }

        var armed = (orders ?? [null])
            .Select(order => new ArmedFailure(
                order?.Status ?? StatusCodes.Status500InternalServerError,
                order?.Count ?? 1,
                order?.DelaySeconds ?? 0))
            .ToList();

        var problem = armed
            .Select((failure, index) => Problem(failure) is { } rule ? $"[{index}].{rule}" : null)
            .FirstOrDefault(rule => rule is not null);

        if (problem is not null)
        {
            return TypedResults.BadRequest(new ApiError(problem));
        }

        if (number is null)
        {
            failures.Arm(armed);
        }
        else
        {
            failures.Arm(number, armed);
        }

        return TypedResults.Ok(SendFailureResponse.From(armed));
    }

    private static string? Problem(ArmedFailure failure) => failure switch
    {
        { Status: < 400 or > 599 } => "status must be from 400 to 599.",
        { Remaining: < 1 } => "count must be 1 or more.",
        { DelaySeconds: < 0 } => "delaySeconds must be 0 or more.",
        _ => null,
    };

    private sealed record FailureOrder(int? Status, int? Count, int? DelaySeconds);
}
