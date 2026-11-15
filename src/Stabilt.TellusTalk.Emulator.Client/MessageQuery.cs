namespace Stabilt.TellusTalk.Emulator.Client;

/// <summary>The list's filters; one left <c>null</c> is not applied.</summary>
public sealed record MessageQuery
{
    public MessageState? State { get; init; }

    public MessageChannel? Channel { get; init; }

    /// <summary>Matches a message's <see cref="CaughtMessage.To"/> as given, on any channel.</summary>
    public string? To { get; init; }

    /// <summary>The read API's <c>q</c>: searches the address, the text, an SMS's sender and another channel's subject.</summary>
    public string? Search { get; init; }
}
