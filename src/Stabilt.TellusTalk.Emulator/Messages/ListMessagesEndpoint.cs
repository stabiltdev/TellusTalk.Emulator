using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Stabilt.TellusTalk.Emulator.Http;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Messages;

internal sealed class ListMessagesEndpoint : IEndpoint
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder builder) => builder.MapGet("api/messages", Handle);

    private static Results<
        Ok<MessageList>,
        BadRequest<ApiError>
    > Handle(
        [FromServices] MessageStore store,
        [FromServices] TimeProvider time,
        string? state,
        string? channel,
        string? to,
        string? q)
    {
        if (!TryReadState(state, out var wanted))
        {
            return TypedResults.BadRequest(new ApiError("state must be sent, scheduled or blocked."));
        }

        if (!TryReadChannel(channel, out var wantedChannel))
        {
            return TypedResults.BadRequest(new ApiError("channel must be sms, email, fax, mms, voice or group."));
        }

        var now = time.GetUtcNow();
        var messages = store
            .List(wanted, to, q, wantedChannel)
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
            "blocked" => (true, MessageState.Blocked),
            _ => (false, null),
        };

        return valid;
    }

    private static bool TryReadChannel(string? channel, out MessageChannel? wanted)
    {
        (var valid, wanted) = channel switch
        {
            null => (true, (MessageChannel?)null),
            "sms" => (true, MessageChannel.Sms),
            "email" => (true, MessageChannel.Email),
            "fax" => (true, MessageChannel.Fax),
            "mms" => (true, MessageChannel.Mms),
            "voice" => (true, MessageChannel.Voice),
            "group" => (true, MessageChannel.Group),
            _ => (false, null),
        };

        return valid;
    }

    private sealed record MessageList(int Total, IReadOnlyList<MessageResponse> Messages);
}
