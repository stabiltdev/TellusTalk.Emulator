using Microsoft.Extensions.Options;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Messages;

public sealed class MaxPartsSettingTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void A_max_number_of_parts_below_one_stops_the_emulator(string setting)
    {
        using var emulator = new EmulatorFactory();
        emulator.Settings["Emulator:SmsMaxParts"] = setting;

        var start = () => emulator.CreateClient();

        start.Should().Throw<OptionsValidationException>();
    }
}
