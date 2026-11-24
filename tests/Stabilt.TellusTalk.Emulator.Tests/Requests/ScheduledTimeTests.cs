using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Requests;

public sealed class ScheduledTimeTests
{
    [Theory]
    [InlineData("2026-11-08T00:10:00+01:00", "2026-11-07T23:10:00Z")]
    [InlineData("2026-11-08T00:10:00Z", "2026-11-08T00:10:00Z")]
    public void A_time_with_an_offset_keeps_it(string value, string expected)
    {
        ScheduledTime.TryParse(value, out var due).Should().BeTrue();
        due.Should().Be(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("2026-11-08T00:10:00", "2026-11-08T00:10:00Z")]
    [InlineData("2026-07-01T12:00", "2026-07-01T12:00:00Z")]
    [InlineData("2026-07-01T12:00:00.5", "2026-07-01T12:00:00.5Z")]
    [InlineData("0001-01-01T00:00:00", "0001-01-01T00:00:00Z")]
    public void A_time_without_an_offset_is_read_as_UTC(string value, string expected)
    {
        ScheduledTime.TryParse(value, out var due).Should().BeTrue();
        due.Should().Be(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
        due.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void A_value_that_is_no_ISO_8601_time_is_not_read()
    {
        ScheduledTime.TryParse("tomorrow", out _).Should().BeFalse();
    }
}
