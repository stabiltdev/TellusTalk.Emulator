using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal sealed class GetSendFailuresEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("api/v1/send-failures", Handle);

    private static Ok<SendFailuresResponse> Handle(SendFailures failures) => TypedResults.Ok(SendFailuresResponse.From(failures.Armed));
}
