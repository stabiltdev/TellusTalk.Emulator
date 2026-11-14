using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Requests;

/// <summary>
/// A parsed JSON value as Python's <c>str()</c> prints it. K8 runs on Python and puts such a value into its refusals, so
/// Python's rules decide the rendering, not only the values that were observed.
/// </summary>
internal static class PythonText
{
    public static string Of(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? value.GetString()!
        : Repr(value);

    private static string Repr(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => "None",
        JsonValueKind.True => "True",
        JsonValueKind.False => "False",
        JsonValueKind.Number => Number(value.GetRawText()),
        JsonValueKind.String => Quoted(value.GetString()!),
        JsonValueKind.Array => "[" + string.Join(", ", value.EnumerateArray().Select(Repr)) + "]",
        _ => Dictionary(value),
    };

    private static string Dictionary(JsonElement value)
    {
        var entries = new List<(string Key, string Text)>();
        foreach (var property in value.EnumerateObject())
        {
            var text = Repr(property.Value);
            var index = entries.FindIndex(entry => string.Equals(entry.Key, property.Name, StringComparison.Ordinal));
            if (index >= 0)
            {
                entries[index] = (property.Name, text);
            }
            else
            {
                entries.Add((property.Name, text));
            }
        }

        return "{" + string.Join(", ", entries.Select(entry => $"{Quoted(entry.Key)}: {entry.Text}")) + "}";
    }

    private static string Number(string raw)
    {
        if (raw.AsSpan().IndexOfAny('.', 'e', 'E') < 0)
        {
            return BigInteger.Parse(raw, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        }

        return Float(double.Parse(raw, CultureInfo.InvariantCulture));
    }

    private static string Float(double value)
    {
        if (double.IsInfinity(value))
        {
            return value < 0 ? "-inf" : "inf";
        }

        var sign = double.IsNegative(value) ? "-" : string.Empty;
        if (value == 0)
        {
            return sign + "0.0";
        }

        var (digits, exponent) = Shortest(Math.Abs(value));

        if (exponent is < -4 or >= 16)
        {
            var mantissa = digits.Length == 1 ? digits : $"{digits[0]}.{digits[1..]}";
            var exponentSign = exponent < 0 ? '-' : '+';

            return $"{sign}{mantissa}e{exponentSign}{Math.Abs(exponent):00}";
        }

        if (exponent < 0)
        {
            return $"{sign}0.{new string('0', -exponent - 1)}{digits}";
        }

        var integer = digits.Length > exponent + 1
            ? digits[..(exponent + 1)]
            : digits.PadRight(exponent + 1, '0');
        var fraction = digits.Length > exponent + 1 ? digits[(exponent + 1)..] : "0";

        return $"{sign}{integer}.{fraction}";
    }

    /// <summary>The shortest digits that read back as the same double, and the decimal exponent of the first digit.</summary>
    private static (string Digits, int Exponent) Shortest(double value)
    {
        var text = value.ToString("R", CultureInfo.InvariantCulture);
        var exponent = 0;
        var e = text.IndexOf('E', StringComparison.Ordinal);
        if (e >= 0)
        {
            exponent = int.Parse(text[(e + 1)..], CultureInfo.InvariantCulture);
            text = text[..e];
        }

        var point = text.IndexOf('.', StringComparison.Ordinal);
        var integerLength = point >= 0 ? point : text.Length;
        var all = text.Replace(".", string.Empty, StringComparison.Ordinal);
        var leadingZeros = all.Length - all.TrimStart('0').Length;
        var digits = all.Trim('0');

        return (digits, exponent + integerLength - 1 - leadingZeros);
    }

    private static string Quoted(string value)
    {
        var quote = value.Contains('\'', StringComparison.Ordinal) && !value.Contains('"', StringComparison.Ordinal)
            ? '"'
            : '\'';

        var builder = new StringBuilder().Append(quote);
        for (var index = 0; index < value.Length;)
        {
            if (!Rune.TryGetRuneAt(value, index, out var rune))
            {
                builder.Append(CultureInfo.InvariantCulture, $"\\u{(int)value[index]:x4}");
                index++;
                continue;
            }

            index += rune.Utf16SequenceLength;
            Escape(builder, rune, quote);
        }

        return builder.Append(quote).ToString();
    }

    private static void Escape(StringBuilder builder, Rune rune, char quote)
    {
        switch (rune.Value)
        {
            case '\\':
                builder.Append("\\\\");
                break;
            case '\n':
                builder.Append("\\n");
                break;
            case '\r':
                builder.Append("\\r");
                break;
            case '\t':
                builder.Append("\\t");
                break;
            default:
                if (rune.Value == quote)
                {
                    builder.Append('\\').Append(quote);
                }
                else if (IsPrintable(rune))
                {
                    builder.Append(rune);
                }
                else if (rune.Value < 0x100)
                {
                    builder.Append(CultureInfo.InvariantCulture, $"\\x{rune.Value:x2}");
                }
                else if (rune.Value < 0x10000)
                {
                    builder.Append(CultureInfo.InvariantCulture, $"\\u{rune.Value:x4}");
                }
                else
                {
                    builder.Append(CultureInfo.InvariantCulture, $"\\U{rune.Value:x8}");
                }

                break;
        }
    }

    /// <summary>Python's <c>str.isprintable</c>: a space is printable, every other separator and every non-graphic category is not.</summary>
    private static bool IsPrintable(Rune rune) => rune.Value == ' ' || Rune.GetUnicodeCategory(rune) is not
        (UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate or UnicodeCategory.PrivateUse
        or UnicodeCategory.OtherNotAssigned or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator
        or UnicodeCategory.SpaceSeparator);
}
