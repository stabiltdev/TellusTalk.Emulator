using System.Globalization;
using Stabilt.TellusTalk.Emulator.Ids;

namespace Stabilt.TellusTalk.Emulator.Tests.Ids;

public sealed class CreateVersion1Tests
{
    private static readonly DateTimeOffset Instant = new(2026, 11, 7, 22, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset GregorianReform = new(1582, 10, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_id_is_a_version_1_UUID_in_the_layout_of_TellusTalks_ids()
    {
        var id = Guid.CreateVersion1(Instant);

        id.Version.Should().Be(1);
        id.ToString("N").Should().MatchRegex("^[0-9a-f]{12}1[0-9a-f]{3}[89ab][0-9a-f]{15}$");
    }

    [Fact]
    public void Ids_minted_at_the_same_instant_are_distinct()
    {
        var ids = Enumerable
            .Range(0, 1000)
            .Select(_ => Guid.CreateVersion1(Instant))
            .ToList();

        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void An_id_encodes_the_time_in_100_nanosecond_ticks_since_the_Gregorian_reform()
    {
        Ticks(Guid.CreateVersion1(Instant)).Should().Be((Instant - GregorianReform).Ticks);
    }

    [Fact]
    public void An_id_minted_without_a_time_takes_the_system_clock()
    {
        var before = TimeProvider.System.GetUtcNow();
        var id = Guid.CreateVersion1();
        var after = TimeProvider.System.GetUtcNow();

        Ticks(id).Should().BeInRange((before - GregorianReform).Ticks, (after - GregorianReform).Ticks);
    }

    [Fact]
    public void A_random_node_carries_the_multicast_bit_that_marks_it_as_no_network_address()
    {
        var firstNodeOctet = byte.Parse(Guid.CreateVersion1(Instant).ToString("N")[20..22], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        (firstNodeOctet & 0x01).Should().Be(1);
    }

    private static long Ticks(Guid id)
    {
        var hex = id.ToString("N");

        return long.Parse(hex[13..16] + hex[8..12] + hex[..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
