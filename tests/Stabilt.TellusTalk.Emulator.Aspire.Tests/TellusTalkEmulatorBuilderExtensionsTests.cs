using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging.Abstractions;

namespace Stabilt.TellusTalk.Emulator.Aspire.Tests;

public sealed class TellusTalkEmulatorBuilderExtensionsTests
{
    [Fact]
    public void The_emulator_runs_the_published_image_of_the_packages_own_version()
    {
        var builder = DistributedApplication.CreateBuilder();

        var emulator = builder.AddTellusTalkEmulator("tellustalk").Resource;

        var image = emulator.Annotations.OfType<ContainerImageAnnotation>().Single();
        image.Registry.Should().Be("ghcr.io");
        image.Image.Should().Be("stabiltdev/tellustalk-emulator");
        image.Tag.Should().Be(typeof(TellusTalkEmulatorResource).Assembly.GetName().Version!.ToString(3));
    }

    [Fact]
    public void The_emulator_serves_http_from_its_port_8080()
    {
        var builder = DistributedApplication.CreateBuilder();

        var emulator = builder.AddTellusTalkEmulator("tellustalk").Resource;

        var endpoint = emulator.Annotations.OfType<EndpointAnnotation>().Single();
        endpoint.Name.Should().Be("http");
        endpoint.UriScheme.Should().Be("http");
        endpoint.TargetPort.Should().Be(8080);
    }

    [Fact]
    public void The_emulator_is_healthy_when_its_health_endpoint_answers()
    {
        var builder = DistributedApplication.CreateBuilder();

        var emulator = builder.AddTellusTalkEmulator("tellustalk").Resource;

        emulator.Annotations.OfType<HealthCheckAnnotation>().Should().ContainSingle();
    }

    [Fact]
    public async Task The_settings_reach_the_emulator_as_its_environment()
    {
        var builder = DistributedApplication.CreateBuilder();

        var emulator = builder
            .AddTellusTalkEmulator("tellustalk")
            .WithAccountSender("Acme")
            .WithFailingNumbers("+46701740698", "+46701740699")
            .WithReportDelay(TimeSpan.FromSeconds(3))
            .WithRetentionLimit(200)
            .Resource;

        var environment = await EnvironmentOf(emulator);

        environment.Should().Contain([
            KeyValuePair.Create("Emulator__AccountSender", "Acme"),
            KeyValuePair.Create("Emulator__FailingNumbers", "+46701740698, +46701740699"),
            KeyValuePair.Create("Emulator__ReportDelaySeconds", "3"),
            KeyValuePair.Create("Emulator__RetentionLimit", "200"),
        ]);
    }

    [Fact]
    public async Task A_reference_gives_the_app_the_emulators_address_as_TellusTalks_base_url()
    {
        var builder = DistributedApplication.CreateBuilder();
        var emulator = builder.AddTellusTalkEmulator("tellustalk");

        var app = builder
            .AddContainer("app", "app-image")
            .WithReference(emulator)
            .Resource;

        var environment = await EnvironmentOf(app);

        environment.Should().Contain(KeyValuePair.Create("TellusTalk__BaseUrl", "{tellustalk.bindings.http.url}"));
    }

    [Fact]
    public async Task A_reference_can_name_the_apps_own_setting()
    {
        var builder = DistributedApplication.CreateBuilder();
        var emulator = builder.AddTellusTalkEmulator("tellustalk");

        var app = builder
            .AddContainer("app", "app-image")
            .WithReference(emulator, "Sms__BaseAddress")
            .Resource;

        var environment = await EnvironmentOf(app);

        environment.Should().ContainKey("Sms__BaseAddress").And.NotContainKey("TellusTalk__BaseUrl");
    }

    [Fact]
    public void A_negative_report_delay_is_refused()
    {
        var emulator = DistributedApplication
            .CreateBuilder()
            .AddTellusTalkEmulator("tellustalk");

        FluentActions.Invoking(() => emulator.WithReportDelay(TimeSpan.FromSeconds(-1))).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void A_report_delay_beyond_what_fits_in_whole_seconds_is_refused()
    {
        var emulator = DistributedApplication
            .CreateBuilder()
            .AddTellusTalkEmulator("tellustalk");

        FluentActions.Invoking(() => emulator.WithReportDelay(TimeSpan.MaxValue)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void A_retention_limit_below_one_is_refused()
    {
        var emulator = DistributedApplication
            .CreateBuilder()
            .AddTellusTalkEmulator("tellustalk");

        FluentActions.Invoking(() => emulator.WithRetentionLimit(0)).Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>The resource's environment as publishing writes it, where an endpoint is a placeholder rather than an address.</summary>
    private static async Task<Dictionary<string, string>> EnvironmentOf(IResource resource)
    {
        var configuration = await ExecutionConfigurationBuilder
            .Create(resource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(
                new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish),
                NullLogger.Instance,
                TestContext.Current.CancellationToken);

        return configuration.EnvironmentVariables.ToDictionary(StringComparer.Ordinal);
    }
}
