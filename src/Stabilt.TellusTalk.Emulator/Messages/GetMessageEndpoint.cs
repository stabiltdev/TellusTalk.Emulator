using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class GetMessageEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("api/v1/messages/{id}", Handle);

    private static Results<Ok<MessageResponse>, NotFound> Handle(MessageStore store, TimeProvider time, string id) =>
        store.Find(id) is { } message
            ? TypedResults.Ok(MessageResponse.From(message, time.GetUtcNow(), store.Reports(message.Id)))
            : TypedResults.NotFound();
}
