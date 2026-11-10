using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal sealed class GetSendFailuresEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("api/v1/send-failures/{number?}", Handle);

    /// <summary>Without a number, what is armed for any send; a number's failures are read at its own path.</summary>
    private static Results<Ok<List<SendFailureResponse>>, BadRequest<ApiError>> Handle(SendFailures failures, string? number)
    {
        if (FailureNumber.Refusal(number) is { } refusal)
        {
            return refusal;
        }

        return TypedResults.Ok(SendFailureResponse.From(number is null ? failures.Armed() : failures.Armed(number)));
    }
}
