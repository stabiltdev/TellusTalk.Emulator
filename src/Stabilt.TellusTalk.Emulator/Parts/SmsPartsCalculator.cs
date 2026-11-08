namespace Stabilt.TellusTalk.Emulator.Parts;

internal static class SmsPartsCalculator
{
    private const int GsmSinglePart = 160;
    private const int GsmSplitPart = 153;
    private const int Ucs2SinglePart = 70;
    private const int Ucs2SplitPart = 67;
    private const int BinaryPart = 140;

    /// <summary>The requested set, or, for <c>optimize</c> and anything undocumented, the cheapest one that fits.</summary>
    public static SmsCharacterSet Choose(string text, string? requested) => requested switch
    {
        "binary" => SmsCharacterSet.Binary,
        "unicode" => SmsCharacterSet.Ucs2,
        "gsm" => SmsCharacterSet.Gsm,
        _ => text.All(c => GsmAlphabet.IsBasic(c) || GsmAlphabet.IsExtension(c)) ? SmsCharacterSet.Gsm : SmsCharacterSet.Ucs2,
    };

    /// <summary>The room in each part of a split message, in the character set's unit.</summary>
    public static int SplitPartCapacity(SmsCharacterSet characterSet) => characterSet switch
    {
        SmsCharacterSet.Gsm => GsmSplitPart,
        SmsCharacterSet.Ucs2 => Ucs2SplitPart,
        _ => BinaryPart,
    };

    public static SmsParts Calculate(string text, SmsCharacterSet characterSet, string? udh) => characterSet switch
    {
        SmsCharacterSet.Gsm => Split(characterSet, GsmUnits(text), GsmSinglePart, GsmSplitPart),
        SmsCharacterSet.Ucs2 => Split(characterSet, Ucs2Units(text), Ucs2SinglePart, Ucs2SplitPart),
        _ => BinaryParts(text, udh),
    };

    /// <summary>
    /// A text that fits one part is sent whole; a longer one is split into smaller parts, since each then carries a
    /// concatenation header. A unit that does not fit what is left of a part starts the next one, never split.
    /// </summary>
    private static SmsParts Split(SmsCharacterSet characterSet, List<Unit> units, int singlePart, int splitPart)
    {
        var length = units.Sum(unit => unit.Cost);
        if (length <= singlePart)
        {
            return new SmsParts(characterSet, length, [0], singlePart - length);
        }

        var starts = new List<int> { 0 };
        var used = 0;
        foreach (var unit in units)
        {
            if (used + unit.Cost > splitPart)
            {
                starts.Add(unit.Index);
                used = 0;
            }

            used += unit.Cost;
        }

        return new SmsParts(characterSet, length, starts, splitPart - used);
    }

    /// <summary>A character outside both tables, possible only when GSM is forced, is counted as one septet.</summary>
    private static List<Unit> GsmUnits(string text) => text
        .Select((c, index) => new Unit(index, GsmAlphabet.IsExtension(c) ? 2 : 1))
        .ToList();

    private static List<Unit> Ucs2Units(string text)
    {
        var units = new List<Unit>();
        for (var index = 0; index < text.Length; index++)
        {
            if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                units.Add(new Unit(index, 2));
                index++;
            }
            else
            {
                units.Add(new Unit(index, 1));
            }
        }

        return units;
    }

    /// <summary>How binary content and a user data header share a part is not documented; this counts bytes only.</summary>
    private static SmsParts BinaryParts(string text, string? udh)
    {
        var length = (text.Length / 2) + ((udh?.Length ?? 0) / 2);
        var count = Math.Max(1, (length + BinaryPart - 1) / BinaryPart);
        var starts = Enumerable
            .Range(0, count)
            .Select(part => part * BinaryPart)
            .ToList();

        return new SmsParts(SmsCharacterSet.Binary, length, starts, (count * BinaryPart) - length);
    }

    private sealed record Unit(int Index, int Cost);
}
