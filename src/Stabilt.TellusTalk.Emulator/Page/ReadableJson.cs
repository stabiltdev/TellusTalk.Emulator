using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Page;

/// <summary>
/// JSON written to be read, indented or on one line, with every character as itself except those JSON requires escaped
/// and the control characters a reader could not see. Strings and names are rewritten from their text as sent rather
/// than read with <see cref="JsonElement.GetString"/>, which throws on half a surrogate pair.
/// </summary>
internal static class ReadableJson
{
    public static string Write(string json)
    {
        using var document = JsonDocument.Parse(json);

        return Write(document.RootElement, 0);
    }

    public static string WriteOnOneLine(JsonElement element) => Write(element, null);

    /// <summary>A <paramref name="depth"/> of <see langword="null"/> writes the value on one line.</summary>
    private static string Write(JsonElement element, int? depth)
    {
        var readable = new StringBuilder();
        Value(readable, element, depth);

        return readable.ToString();
    }

    private static void Value(StringBuilder readable, JsonElement element, int? depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                Members(readable, element, depth);
                break;
            case JsonValueKind.Array:
                Items(readable, element, depth);
                break;
            case JsonValueKind.String:
                var sent = element.GetRawText();
                Text(readable, sent.AsSpan(1, sent.Length - 2));
                break;
            default:
                readable.Append(element.GetRawText());
                break;
        }
    }

    private static void Members(StringBuilder readable, JsonElement element, int? depth)
    {
        if (element.GetPropertyCount() == 0)
        {
            readable.Append("{}");
            return;
        }

        readable.Append('{');
        var first = true;
        foreach (var property in element.EnumerateObject())
        {
            Before(readable, first, depth + 1);
            Text(readable, Encoding.UTF8.GetString(JsonMarshal.GetRawUtf8PropertyName(property)));
            readable.Append(": ");
            Value(readable, property.Value, depth + 1);
            first = false;
        }

        NewLine(readable, depth);
        readable.Append('}');
    }

    private static void Items(StringBuilder readable, JsonElement element, int? depth)
    {
        if (element.GetArrayLength() == 0)
        {
            readable.Append("[]");
            return;
        }

        readable.Append('[');
        var first = true;
        foreach (var item in element.EnumerateArray())
        {
            Before(readable, first, depth + 1);
            Value(readable, item, depth + 1);
            first = false;
        }

        NewLine(readable, depth);
        readable.Append(']');
    }

    /// <summary>What goes before a member or an item: the comma after the one before it, then its line or a space.</summary>
    private static void Before(StringBuilder readable, bool first, int? depth)
    {
        if (!first)
        {
            readable.Append(depth is null ? ", " : ",");
        }

        NewLine(readable, depth);
    }

    /// <summary>Nothing on one line.</summary>
    private static void NewLine(StringBuilder readable, int? depth)
    {
        if (depth is { } indent)
        {
            readable
                .Append('\n')
                .Append(' ', indent * 2);
        }
    }

    /// <summary>A string or a name, quoted, from its text between the quotes as sent.</summary>
    private static void Text(StringBuilder readable, ReadOnlySpan<char> sent)
    {
        readable.Append('"');
        var at = 0;
        while (at < sent.Length)
        {
            if (sent[at] != '\\')
            {
                Character(readable, sent[at]);
                at++;
            }
            else if (sent[at + 1] != 'u')
            {
                Character(readable, Unescaped(sent[at + 1]));
                at += 2;
            }
            else if (CodeUnit(sent, at) is var high
                     && char.IsHighSurrogate(high)
                     && IsCodeUnitEscape(sent, at + 6)
                     && CodeUnit(sent, at + 6) is var low
                     && char.IsLowSurrogate(low))
            {
                readable
                    .Append(high)
                    .Append(low);
                at += 12;
            }
            else if (char.IsSurrogate(CodeUnit(sent, at)))
            {
                readable.Append(sent.Slice(at, 6));
                at += 6;
            }
            else
            {
                Character(readable, CodeUnit(sent, at));
                at += 6;
            }
        }

        readable.Append('"');
    }

    private static void Character(StringBuilder readable, char character)
    {
        if (ShortEscape(character) is { } letter)
        {
            readable
                .Append('\\')
                .Append(letter);
        }
        else if (char.IsControl(character))
        {
            readable.Append(CultureInfo.InvariantCulture, $@"\u{(int)character:X4}");
        }
        else
        {
            readable.Append(character);
        }
    }

    /// <summary>The character a two-character escape stands for: the letter after the backslash, or itself.</summary>
    private static char Unescaped(char letter) => letter switch
    {
        'b' => '\b',
        'f' => '\f',
        'n' => '\n',
        'r' => '\r',
        't' => '\t',
        _ => letter,
    };

    private static char? ShortEscape(char character) => character switch
    {
        '"' => '"',
        '\\' => '\\',
        '\b' => 'b',
        '\f' => 'f',
        '\n' => 'n',
        '\r' => 'r',
        '\t' => 't',
        _ => null,
    };

    private static bool IsCodeUnitEscape(ReadOnlySpan<char> sent, int at) =>
        at + 6 <= sent.Length && sent[at] == '\\' && sent[at + 1] == 'u';

    private static char CodeUnit(ReadOnlySpan<char> sent, int at) =>
        (char)ushort.Parse(sent.Slice(at + 2, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
}
