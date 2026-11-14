namespace Stabilt.TellusTalk.Emulator.Testcontainers;

/// <summary>The user's Sender setting at TellusTalk: what a send without sender fields shows as its sender.</summary>
public abstract record SmsSender
{
    internal abstract string Setting { get; }

    private protected static string NotBlank(string value, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);

        return value;
    }
}

/// <summary>The reply number TellusTalk assigns, set with <c>WithReplyNumber</c>.</summary>
public sealed record ReplySmsSender : SmsSender
{
    internal override string Setting => "replysms";
}

/// <summary>A number of the user's own.</summary>
/// <param name="Number">The number shown as the sender.</param>
public sealed record NumberSender(string Number) : SmsSender
{
    /// <summary>The number shown as the sender.</summary>
    public string Number { get; } = NotBlank(Number, nameof(Number));

    internal override string Setting => $"number:{Number}";
}

/// <summary>A name of the user's own.</summary>
/// <param name="Name">The name shown as the sender.</param>
public sealed record TextSender(string Name) : SmsSender
{
    /// <summary>The name shown as the sender.</summary>
    public string Name { get; } = NotBlank(Name, nameof(Name));

    internal override string Setting => $"text:{Name}";
}

/// <summary>The send's subject field, which shows the reply number when the request sets none.</summary>
public sealed record SubjectSender : SmsSender
{
    internal override string Setting => "subject";
}
