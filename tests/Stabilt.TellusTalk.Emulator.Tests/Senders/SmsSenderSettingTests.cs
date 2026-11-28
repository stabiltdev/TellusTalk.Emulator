using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Stabilt.TellusTalk.Emulator.Tests.Senders;

/// <summary>
/// Read from the options rather than a started host: a host that fails its validation in memory may surface as a
/// disposed service provider instead of the validation's own exception.
/// </summary>
public sealed class SmsSenderSettingTests
{
    [Fact]
    public void An_sms_sender_that_is_no_setting_is_refused()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Emulator:SmsSender"] = "text:",
            })
            .Build();

        using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddEmulatorOptions()
            .BuildServiceProvider();

        var read = () => provider.GetRequiredService<IOptions<EmulatorOptions>>().Value;

        read.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle()
            .Which.Should().Be("Emulator__SmsSender must be replysms, text:<name>, number:<number> or subject.");
    }
}
