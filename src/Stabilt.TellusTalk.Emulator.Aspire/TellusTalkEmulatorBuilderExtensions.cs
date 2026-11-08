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

    /// <summary>The sender the TellusTalk account is configured with, shown when a request sets none; a number stands in for two-way SMS.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithAccountSender(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        string sender)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(sender);

        return builder.WithEnvironment("Emulator__AccountSender", sender);
    }

    /// <summary>The numbers whose delivery reports say the operator could not deliver.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithFailingNumbers(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        params string[] numbers)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(numbers);

        return builder.WithEnvironment("Emulator__FailingNumbers", string.Join(", ", numbers));
    }

    /// <summary>How long after a message is sent, or falls due, its delivery report goes out, in whole seconds.</summary>
    public static IResourceBuilder<TellusTalkEmulatorResource> WithReportDelay(
        this IResourceBuilder<TellusTalkEmulatorResource> builder,
        TimeSpan delay)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);

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
