using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class DeleteMessagesEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapDelete("api/v1/messages", Handle);

    private static NoContent Handle(MessageStore store)
    {
        store.Clear();

        return TypedResults.NoContent();
    }
}
