using System.Globalization;
using System.Reflection;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Images;

namespace Stabilt.TellusTalk.Emulator.Testcontainers;

/// <summary>
/// Builds a TellusTalk Emulator container, started once its health endpoint answers. Give the app under test its address
/// with <see cref="TellusTalkEmulatorContainer.GetBaseAddress"/>.
/// </summary>
public sealed class TellusTalkEmulatorBuilder : ContainerBuilder<TellusTalkEmulatorBuilder, TellusTalkEmulatorContainer, ContainerConfiguration>
{
    /// <summary>The port in the container that serves TellusTalk's send API, the emulator's page and its read API.</summary>
    public const ushort TellusTalkEmulatorPort = 8080;

    private const string Image = "ghcr.io/stabiltdev/tellustalk-emulator";
    private const string FailingNumbers = "Emulator__FailingNumbers";

    /// <summary>The package's own version without its build metadata: one release publishes the image and the package.</summary>
    private static readonly string ImageTag = typeof(TellusTalkEmulatorBuilder).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
        .InformationalVersion
        .Split('+')[0];

    /// <summary>Runs the emulator image of the package's own version, <c>ghcr.io/stabiltdev/tellustalk-emulator:&lt;version&gt;</c>.</summary>
    public TellusTalkEmulatorBuilder()
        : this($"{Image}:{ImageTag}")
    {
    }

    /// <summary>Runs another image of the emulator.</summary>
    /// <param name="image">The image's full name with its tag, such as <c>ghcr.io/stabiltdev/tellustalk-emulator:0.4.1</c>.</param>
    public TellusTalkEmulatorBuilder(string image)
        : this(new ContainerConfiguration())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(image);

        DockerResourceConfiguration = Init().WithImage(new DockerImage(image)).DockerResourceConfiguration;
    }

    private TellusTalkEmulatorBuilder(ContainerConfiguration configuration)
        : base(configuration)
    {
        DockerResourceConfiguration = configuration;
    }

    /// <inheritdoc />
    protected override ContainerConfiguration DockerResourceConfiguration { get; }

    /// <summary>The user's Sender setting, shown as the sender when a request sets none.</summary>
    public TellusTalkEmulatorBuilder WithSmsSender(TellusTalkSmsSender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        return WithEnvironment("Emulator__SmsSender", sender.Setting);
    }

    /// <summary>The number TellusTalk assigns for replies, shown for <c>replysms</c>.</summary>
    public TellusTalkEmulatorBuilder WithReplyNumber(string number)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);

        return WithEnvironment("Emulator__ReplyNumber", number);
    }

    /// <summary>The sender the TellusTalk account is configured with, shown when a request sets none; a number stands in for two-way SMS.</summary>
    [Obsolete("Use WithSmsSender with TellusTalkSmsSender.Text for a name, or TellusTalkSmsSender.ReplySms and WithReplyNumber for a two-way number.")]
    public TellusTalkEmulatorBuilder WithAccountSender(string sender)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sender);

        return WithEnvironment("Emulator__AccountSender", sender);
    }

    /// <summary>Adds numbers whose delivery reports say the operator could not deliver; each call adds to the earlier ones.</summary>
    public TellusTalkEmulatorBuilder WithFailingNumbers(params string[] numbers)
    {
        ArgumentNullException.ThrowIfNull(numbers);

        if (numbers.Length == 0)
        {
            return this;
        }

        var added = string.Join(", ", numbers);

        return DockerResourceConfiguration.Environments?.TryGetValue(FailingNumbers, out var earlier) == true && earlier.Length > 0
            ? WithEnvironment(FailingNumbers, $"{earlier}, {added}")
            : WithEnvironment(FailingNumbers, added);
    }

    /// <summary>How long after a message is sent, or falls due, its delivery report goes out, in whole seconds.</summary>
    public TellusTalkEmulatorBuilder WithReportDelay(TimeSpan delay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(delay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(delay, TimeSpan.FromSeconds(int.MaxValue));

        return WithEnvironment(
            "Emulator__ReportDelaySeconds",
            ((int)delay.TotalSeconds).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>How many messages the emulator keeps; the oldest go first.</summary>
    public TellusTalkEmulatorBuilder WithRetentionLimit(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        return WithEnvironment("Emulator__RetentionLimit", limit.ToString(CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public override TellusTalkEmulatorContainer Build()
    {
        Validate();

        return new TellusTalkEmulatorContainer(DockerResourceConfiguration);
    }

    /// <inheritdoc />
    protected override TellusTalkEmulatorBuilder Init() => base
        .Init()
        .WithPortBinding(TellusTalkEmulatorPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait
            .ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request
                .ForPort(TellusTalkEmulatorPort)
                .ForPath("/api/v1/health")));

    /// <inheritdoc />
    protected override TellusTalkEmulatorBuilder Clone(IResourceConfiguration<CreateContainerParameters> resourceConfiguration) =>
        Merge(DockerResourceConfiguration, new ContainerConfiguration(resourceConfiguration));

    /// <inheritdoc />
    protected override TellusTalkEmulatorBuilder Clone(IContainerConfiguration resourceConfiguration) =>
        Merge(DockerResourceConfiguration, new ContainerConfiguration(resourceConfiguration));

    /// <inheritdoc />
    protected override TellusTalkEmulatorBuilder Merge(ContainerConfiguration oldValue, ContainerConfiguration newValue) =>
        new(new ContainerConfiguration(oldValue, newValue));
}
