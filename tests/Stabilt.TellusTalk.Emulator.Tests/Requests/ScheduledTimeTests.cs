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
    [InlineData("2026-11-08T00:10:00", "2026-11-07T23:10:00Z")]
    [InlineData("2026-07-01T12:00:00", "2026-07-01T10:00:00Z")]
    public void A_time_without_an_offset_is_read_as_Swedish_time(string value, string expected)
    {
        ScheduledTime.TryParse(value, out var due).Should().BeTrue();
        due.Should().Be(DateTimeOffset.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("0001-01-01T00:00:00")]
    [InlineData("1800-01-01T00:00:00")]
    public void A_time_too_early_for_an_offset_is_read_as_long_past(string value)
    {
        ScheduledTime.TryParse(value, out var due).Should().BeTrue();
        due.Should().BeBefore(new DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_time_inside_the_spring_forward_gap_is_read()
    {
        ScheduledTime.TryParse("2026-03-29T02:30:00", out var due).Should().BeTrue();
        due.Should().Be(new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void A_value_that_is_no_ISO_8601_time_is_not_read()
    {
        ScheduledTime.TryParse("tomorrow", out _).Should().BeFalse();
    }
}
