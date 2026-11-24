using Stabilt.TellusTalk.Emulator.Parts;

namespace Stabilt.TellusTalk.Emulator.Tests.Parts;

public sealed class SmsPartsCalculatorTests
{
    private const string Welcome =
        "Välkommen till tjänsten! Vi har tagit emot din intresseanmälan och en av våra koordinatorer ringer dig inom två " +
        "arbetsdagar. Logga in med e-post för att följa ärendet och se vilka dokument vi behöver från dig.";

    [Fact]
    public void A_Swedish_text_of_209_GSM_characters_takes_two_parts_with_97_left()
    {
        var parts = SmsPartsCalculator.Calculate(Welcome, SmsCharacterSet.Gsm, udh: null);

        parts.Should().BeEquivalentTo(new SmsParts(SmsCharacterSet.Gsm, 209, [0, 153], 97));
        Welcome[153..].Should().StartWith("följa ärendet");
    }

    [Fact]
    public void One_GSM_part_holds_160_septets()
    {
        var parts = SmsPartsCalculator.Calculate(new string('a', 160), SmsCharacterSet.Gsm, udh: null);

        parts.Starts.Should().Equal(0);
        parts.LeftInLastPart.Should().Be(0);
    }

    [Fact]
    public void A_longer_GSM_text_splits_into_parts_of_153()
    {
        var parts = SmsPartsCalculator.Calculate(new string('a', 161), SmsCharacterSet.Gsm, udh: null);

        parts.Starts.Should().Equal(0, 153);
        parts.LeftInLastPart.Should().Be(145);
    }

    [Fact]
    public void An_extension_character_takes_two_septets()
    {
        var parts = SmsPartsCalculator.Calculate(new string('a', 159) + "€", SmsCharacterSet.Gsm, udh: null);

        parts.Length.Should().Be(161);
        parts.Count.Should().Be(2);
    }

    [Fact]
    public void An_extension_character_is_never_split_across_parts()
    {
        var parts = SmsPartsCalculator.Calculate(new string('a', 152) + "€" + new string('b', 10), SmsCharacterSet.Gsm, udh: null);

        parts.Starts.Should().Equal(0, 152);
        parts.LeftInLastPart.Should().Be(141);
    }

    [Fact]
    public void One_UCS2_part_holds_70_code_units()
    {
        var parts = SmsPartsCalculator.Calculate(new string('å', 70), SmsCharacterSet.Ucs2, udh: null);

        parts.Starts.Should().Equal(0);
        parts.LeftInLastPart.Should().Be(0);
    }

    [Fact]
    public void A_longer_UCS2_text_splits_into_parts_of_67()
    {
        var parts = SmsPartsCalculator.Calculate(new string('å', 71), SmsCharacterSet.Ucs2, udh: null);

        parts.Starts.Should().Equal(0, 67);
        parts.LeftInLastPart.Should().Be(63);
    }

    [Fact]
    public void A_surrogate_pair_is_never_split_across_parts()
    {
        var parts = SmsPartsCalculator.Calculate(new string('a', 66) + "😊" + new string('b', 5), SmsCharacterSet.Ucs2, udh: null);

        parts.Starts.Should().Equal(0, 66);
    }

    [Fact]
    public void An_empty_text_is_one_empty_GSM_part()
    {
        var parts = SmsPartsCalculator.Calculate(string.Empty, SmsPartsCalculator.Choose(string.Empty, requested: null), udh: null);

        parts.Should().BeEquivalentTo(new SmsParts(SmsCharacterSet.Gsm, 0, [0], 160));
    }

    [Fact]
    public void A_binary_message_counts_its_data_and_header_bytes()
    {
        var parts = SmsPartsCalculator.Calculate("48656C6C6F", SmsCharacterSet.Binary, udh: "050003CC0201");

        parts.Should().BeEquivalentTo(new SmsParts(SmsCharacterSet.Binary, 11, [0], 129));
    }

    [Fact]
    public void A_GSM_text_cut_to_one_part_keeps_160_septets()
    {
        var text = new string('a', 320);

        SmsPartsCalculator.Cut(text, SmsPartsCalculator.Calculate(text, SmsCharacterSet.Gsm, udh: null), maxParts: 1)
            .Should().Be(new string('a', 160));
    }

    [Fact]
    public void A_cut_never_splits_an_extension_character()
    {
        var text = new string('a', 159) + "€" + new string('b', 10);

        SmsPartsCalculator.Cut(text, SmsPartsCalculator.Calculate(text, SmsCharacterSet.Gsm, udh: null), maxParts: 1)
            .Should().Be(new string('a', 159));
    }

    [Fact]
    public void A_UCS2_text_cut_to_one_part_keeps_70_code_units_and_never_splits_a_surrogate_pair()
    {
        var text = new string('å', 69) + "😊" + new string('b', 10);

        SmsPartsCalculator.Cut(text, SmsPartsCalculator.Calculate(text, SmsCharacterSet.Ucs2, udh: null), maxParts: 1)
            .Should().Be(new string('å', 69));
    }

    [Fact]
    public void A_text_cut_to_two_parts_keeps_them_as_it_splits()
    {
        var text = new string('a', 400);

        SmsPartsCalculator.Cut(text, SmsPartsCalculator.Calculate(text, SmsCharacterSet.Gsm, udh: null), maxParts: 2)
            .Should().Be(new string('a', 306));
    }

    [Fact]
    public void A_text_within_its_parts_is_not_cut()
    {
        var parts = SmsPartsCalculator.Calculate(Welcome, SmsCharacterSet.Gsm, udh: null);

        SmsPartsCalculator.Cut(Welcome, parts, maxParts: 2).Should().Be(Welcome);
    }

    [Theory]
    [InlineData(Welcome, null, nameof(SmsCharacterSet.Gsm))]
    [InlineData(Welcome, "optimize", nameof(SmsCharacterSet.Gsm))]
    [InlineData("Tack 😊", null, nameof(SmsCharacterSet.Ucs2))]
    [InlineData("Tack 😊", "gsm", nameof(SmsCharacterSet.Ucs2))]
    [InlineData("Hej", "gsm", nameof(SmsCharacterSet.Gsm))]
    [InlineData("Hej", "unicode", nameof(SmsCharacterSet.Ucs2))]
    [InlineData("41", "binary", nameof(SmsCharacterSet.Binary))]
    [InlineData("Hej", "latin1", nameof(SmsCharacterSet.Gsm))]
    public void The_character_set_follows_the_request_or_the_cheapest_that_fits(string text, string? requested, string expected)
    {
        SmsPartsCalculator.Choose(text, requested).Should().Be(Enum.Parse<SmsCharacterSet>(expected));
    }
}
