using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class StreamChangesEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("api/v1/events", Handle);

    private static ServerSentEventsResult<StoreChange> Handle(MessageStore store, CancellationToken cancellationToken) =>
        TypedResults.ServerSentEvents(Changes(store, cancellationToken));

    private static async IAsyncEnumerable<SseItem<StoreChange>> Changes(
        MessageStore store,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var change in store.Changes(cancellationToken))
        {
            yield return new SseItem<StoreChange>(change, "change");
        }
    }
}
