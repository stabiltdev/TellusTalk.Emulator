using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Sending;

internal sealed class DeleteSendFailuresEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapDelete("api/v1/send-failures", Handle);

    private static NoContent Handle(SendFailures failures)
    {
        failures.Clear();

        return TypedResults.NoContent();
    }
}
