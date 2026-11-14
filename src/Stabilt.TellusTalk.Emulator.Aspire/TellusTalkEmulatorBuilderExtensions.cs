using System.Globalization;
using System.Reflection;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>Adds TellusTalk Emulator to an Aspire app and gives other resources its address.</summary>
public static class TellusTalkEmulatorBuilderExtensions
{
    private const string Registry = "ghcr.io";
    private const string Image = "stabiltdev/tellustalk-emulator";
    private const int ContainerPort = 8080;
    private const string FailingNumbers = "Emulator__FailingNumbers";

    /// <summary>The package's own version without its build metadata: one release publishes the image and the package.</summary>
    private static readonly string ImageTag = typeof(TellusTalkEmulatorBuilderExtensions).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion
        .Split('+')[0];

    /// <summary>
    /// Adds TellusTalk Emulator, healthy once its health endpoint answers. Give an app its address with
    /// <see cref="WithReference{TDestination}"/>.
    /// </summary>
    /// <param name="port">The host port, or <see langword="null"/> for one Aspire chooses.</param>
    public static IResourceBuilder<TellusTalkEmulatorResource> AddTellusTalkEmulator(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return builder
            .AddResource(new TellusTalkEmulatorResource(name))
            .WithImage(Image, ImageTag)
            .WithImageRegistry(Registry)
            .WithHttpEndpoint(port: port, targetPort: ContainerPort, name: TellusTalkEmulatorResource.HttpEndpointName)
            .WithHttpHealthCheck("/api/v1/health", endpointName: TellusTalkEmulatorResource.HttpEndpointName);
    }

    /// <summary>The user's Sender setting, shown as the sender when a request sets none.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithSmsSender(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        SmsSender sender)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(sender);

        return builder.WithEnvironment("Emulator__SmsSender", sender.Setting);
    }

    /// <summary>The number TellusTalk assigns for replies, shown for <c>replysms</c>.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithReplyNumber(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        string number)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return builder.WithEnvironment("Emulator__ReplyNumber", number);
    }

    /// <summary>The sender the TellusTalk account is configured with, shown when a request sets none; a number stands in for two-way SMS.</summary>
    [Obsolete("Use WithSmsSender, with a TextSender for a name or a ReplySmsSender and WithReplyNumber for a two-way number.")]
    public static IResourceBuilder<TellusTalkEmulatorResource> WithAccountSender(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        string sender)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(sender);

        return builder.WithEnvironment("Emulator__AccountSender", sender);
    }

    /// <summary>Adds numbers whose delivery reports say the operator could not deliver; each call adds to the earlier ones.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithFailingNumbers(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        params string[] numbers)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(numbers);

        if (numbers.Length == 0)
        {
            return builder;
        }

        var added = string.Join(", ", numbers);

        // Environment callbacks run in the order they were added, so each call sees what earlier calls wrote.
        return builder.WithEnvironment(context =>
            context.EnvironmentVariables[FailingNumbers] =
                context.EnvironmentVariables.TryGetValue(FailingNumbers, out var earlier) && earlier is string { Length: > 0 } written
                    ? $"{written}, {added}"
                    : added);
    }

    /// <summary>How long after a message is sent, or falls due, its delivery report goes out, in whole seconds.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithReportDelay(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(delay, TimeSpan.FromSeconds(int.MaxValue));

        return builder.WithEnvironment(
            "Emulator__ReportDelaySeconds",
            ((int)delay.TotalSeconds).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>How many messages the emulator keeps; the oldest go first.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithRetentionLimit(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        int limit)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        return builder.WithEnvironment("Emulator__RetentionLimit", limit.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Gives the app the emulator's address in <paramref name="environmentVariable"/>, by default
    /// <c>TellusTalk__BaseUrl</c>, the setting <c>TellusTalk:BaseUrl</c>.
    /// </summary>
    public static IResourceBuilder<TDestination> WithReference<TDestination>(
        this IResourceBuilder<TDestination> builder,
        IResourceBuilder<TellusTalkEmulatorResource> emulator,
        string environmentVariable = "TellusTalk__BaseUrl")
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(emulator);
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentVariable);

        return builder.WithEnvironment(environmentVariable, emulator.Resource.HttpEndpoint);
    }
}
