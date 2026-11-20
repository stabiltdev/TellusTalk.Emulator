using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class DeleteMessagesEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapDelete("api/messages", Handle);

    private static NoContent Handle([FromServices] MessageStore store)
    {
        store.Clear();

        return TypedResults.NoContent();
    }
}
