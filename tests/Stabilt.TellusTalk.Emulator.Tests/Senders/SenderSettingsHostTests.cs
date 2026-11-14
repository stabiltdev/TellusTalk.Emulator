using Microsoft.Extensions.Options;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Senders;

public sealed class SenderSettingsHostTests
{
    [Fact]
    public void An_sms_sender_that_is_no_setting_stops_the_emulator()
    {
        using var emulator = new EmulatorFactory();
        emulator.Settings["Emulator:SmsSender"] = "text:";

        var start = () => emulator.CreateClient();

        start.Should().Throw<OptionsValidationException>();
    }
}
