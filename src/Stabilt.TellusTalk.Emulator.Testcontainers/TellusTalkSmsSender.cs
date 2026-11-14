namespace Stabilt.TellusTalk.Emulator.Testcontainers;

/// <summary>The user's Sender setting in TellusTalk: what a send without sender fields shows.</summary>
public sealed class TellusTalkSmsSender
{
    private TellusTalkSmsSender(string setting) => Setting = setting;

    /// <summary>The reply number TellusTalk assigns, set with <c>WithReplyNumber</c>.</summary>
    public static TellusTalkSmsSender ReplySms { get; } = new("replysms");

    /// <summary>The send's subject field, which shows the reply number when the request sets none.</summary>
    public static TellusTalkSmsSender Subject { get; } = new("subject");

    /// <summary>A name of the user's own, shown as the sender.</summary>
    /// <param name="name">The name the phone shows.</param>
    public static TellusTalkSmsSender Text(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new($"text:{name}");
    }

    /// <summary>A number of the user's own, shown as the sender.</summary>
    /// <param name="number">The number the phone shows.</param>
    public static TellusTalkSmsSender Number(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return new($"number:{number}");
    }

    internal string Setting { get; }
}
