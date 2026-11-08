using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class DeleteMessageEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapDelete("api/v1/messages/{id}", Handle);

    private static Results<NoContent, NotFound> Handle(MessageStore store, string id) =>
        store.Remove(id) ? TypedResults.NoContent() : TypedResults.NotFound();
}
