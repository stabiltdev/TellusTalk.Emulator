using Microsoft.AspNetCore.Http.HttpResults;
using Stabilt.TellusTalk.Emulator.Http;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class ListMessagesEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("api/v1/messages", Handle);

    private static Results<Ok<MessageList>, BadRequest<ApiError>> Handle(
        MessageStore store,
        TimeProvider time,
        string? state,
        string? to,
        string? q)
    {
        if (!TryReadState(state, out var wanted))
        {
            return TypedResults.BadRequest(new ApiError("state must be sent or scheduled."));
        }

        var now = time.GetUtcNow();
        var messages = store
            .List(wanted, Number(to), q)
            .Select(message => MessageResponse.From(message, now, store.Reports(message.Id)))
            .ToList();

        return TypedResults.Ok(new MessageList(messages.Count, messages));
    }

    private static bool TryReadState(string? state, out MessageState? wanted)
    {
        (var valid, wanted) = state switch
        {
            null => (true, (MessageState?)null),
            "sent" => (true, MessageState.Sent),
            "scheduled" => (true, MessageState.Scheduled),
            _ => (false, null),
        };

        return valid;
    }

    /// <summary>
    /// A number's <c>+</c>, left unescaped in a query string, arrives as a space; so does a space a person typed. Both
    /// read as the number in its <c>+</c> form.
    /// </summary>
    private static string? Number(string? to)
    {
        if (to is null)
        {
            return null;
        }

        var digits = to.Replace(" ", string.Empty, StringComparison.Ordinal);

        return digits.StartsWith('+') ? digits : "+" + digits;
    }

    private sealed record MessageList(int Total, IReadOnlyList<MessageResponse> Messages);
}
