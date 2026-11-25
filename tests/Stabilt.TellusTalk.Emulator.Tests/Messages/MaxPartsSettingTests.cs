using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Stabilt.TellusTalk.Emulator.Tests.Messages;

/// <summary>
/// Read from the options rather than a started host: a host that fails its validation in memory may surface as a
/// disposed service provider instead of the validation's own exception.
/// </summary>
public sealed class MaxPartsSettingTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void A_max_number_of_parts_below_one_is_refused(string setting)
    {
        using var provider = Provider(setting);

        var read = () => provider.GetRequiredService<IOptions<EmulatorOptions>>().Value;

        read.Should().Throw<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle().Which.Should().Be("Emulator__SmsMaxParts must be 1 or more.");
    }

    [Fact]
    public void Without_a_max_number_of_parts_it_is_eight()
    {
        using var provider = Provider(setting: null);

        provider.GetRequiredService<IOptions<EmulatorOptions>>().Value.SmsMaxParts.Should().Be(8);
    }

    private static ServiceProvider Provider(string? setting)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (setting is not null)
        {
            settings["Emulator:SmsMaxParts"] = setting;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddEmulatorOptions()
            .BuildServiceProvider();
    }
}
