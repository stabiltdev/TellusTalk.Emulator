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
            .WithSmsSender(TellusTalkSmsSender.Text("Acme"))
            .WithReplyNumber("+46701740697")
            .WithFailingNumbers("+46701740698", "+46701740699")
            .WithReportDelay(TimeSpan.FromSeconds(3))
            .WithRetentionLimit(200)
            .Resource;

        var environment = await EnvironmentOf(emulator);

        environment.Should().Contain([
            KeyValuePair.Create("Emulator__SmsSender", "text:Acme"),
            KeyValuePair.Create("Emulator__ReplyNumber", "+46701740697"),
            KeyValuePair.Create("Emulator__FailingNumbers", "+46701740698, +46701740699"),
            KeyValuePair.Create("Emulator__ReportDelaySeconds", "3"),
            KeyValuePair.Create("Emulator__RetentionLimit", "200"),
        ]);
    }

    [Theory]
    [MemberData(nameof(SmsSenders))]
    public async Task Each_sms_sender_is_written_in_TellusTalks_words(TellusTalkSmsSender sender, string setting)
    {
        var emulator = DistributedApplication
            .CreateBuilder()
            .AddTellusTalkEmulator("tellustalk")
            .WithSmsSender(sender)
            .Resource;

        var environment = await EnvironmentOf(emulator);

        environment.Should().Contain(KeyValuePair.Create("Emulator__SmsSender", setting));
    }

    public static TheoryData<TellusTalkSmsSender, string> SmsSenders => new()
    {
        { TellusTalkSmsSender.ReplySms, "replysms" },
        { TellusTalkSmsSender.Number("+46701740697"), "number:+46701740697" },
        { TellusTalkSmsSender.Text("Acme"), "text:Acme" },
        { TellusTalkSmsSender.Subject, "subject" },
    };

    [Fact]
    public async Task The_account_sender_is_written_as_its_setting()
    {
        var builder = DistributedApplication
            .CreateBuilder()
            .AddTellusTalkEmulator("tellustalk");

        // The alias is kept for existing configurations, so it is called here although it is obsolete.
#pragma warning disable CS0618
        var emulator = builder.WithAccountSender("Acme").Resource;
#pragma warning restore CS0618

        var environment = await EnvironmentOf(emulator);

        environment.Should().Contain(KeyValuePair.Create("Emulator__AccountSender", "Acme"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_sender_name_is_refused(string? name)
    {
        FluentActions.Invoking(() => TellusTalkSmsSender.Text(name!)).Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void A_blank_sender_number_is_refused(string? number)
    {
        FluentActions.Invoking(() => TellusTalkSmsSender.Number(number!)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task Failing_numbers_from_several_calls_add_up()
    {
        var emulator = DistributedApplication
            .CreateBuilder()
            .AddTellusTalkEmulator("tellustalk")
            .WithFailingNumbers("+46701740697", "+46701740698")
            .WithFailingNumbers("+46701740699")
            .Resource;

        var environment = await EnvironmentOf(emulator);

        environment.Should().Contain(KeyValuePair.Create("Emulator__FailingNumbers", "+46701740697, +46701740698, +46701740699"));
    }

    [Fact]
    public async Task A_call_without_numbers_adds_none()
    {
        var emulator = DistributedApplication
            .CreateBuilder()
            .AddTellusTalkEmulator("tellustalk")
            .WithFailingNumbers("+46701740698")
            .WithFailingNumbers()
            .Resource;

        var environment = await EnvironmentOf(emulator);

        environment.Should().Contain(KeyValuePair.Create("Emulator__FailingNumbers", "+46701740698"));
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
