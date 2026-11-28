using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Stabilt.TellusTalk.Emulator.Page;

/// <summary>
/// A request written to be read: indented, with every character as itself except those JSON requires escaped and the
/// control characters a reader could not see. Strings and names are rewritten from their text as sent rather than read
/// with <see cref="JsonElement.GetString"/>, which throws on half a surrogate pair.
/// </summary>
internal static class ReadableJson
{
    public static string Write(string json)
    {
        using var document = JsonDocument.Parse(json);
        var readable = new StringBuilder();
        Value(readable, document.RootElement, 0);

        return readable.ToString();
    }

    private static void Value(StringBuilder readable, JsonElement element, int depth)
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

    private static void Members(StringBuilder readable, JsonElement element, int depth)
    {
        if (element.GetPropertyCount() == 0)
        {
            readable.Append("{}");
            return;
        }

        readable.Append('{');
        var separator = "";
        foreach (var property in element.EnumerateObject())
        {
            NewLine(readable.Append(separator), depth + 1);
            Text(readable, Encoding.UTF8.GetString(JsonMarshal.GetRawUtf8PropertyName(property)));
            readable.Append(": ");
            Value(readable, property.Value, depth + 1);
            separator = ",";
        }

        NewLine(readable, depth).Append('}');
    }

    private static void Items(StringBuilder readable, JsonElement element, int depth)
    {
        if (element.GetArrayLength() == 0)
        {
            readable.Append("[]");
            return;
        }

        readable.Append('[');
        var separator = "";
        foreach (var item in element.EnumerateArray())
        {
            NewLine(readable.Append(separator), depth + 1);
            Value(readable, item, depth + 1);
            separator = ",";
        }

        NewLine(readable, depth).Append(']');
    }

    private static StringBuilder NewLine(StringBuilder readable, int depth) => readable.Append('\n').Append(' ', depth * 2);

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
                readable.Append(high).Append(low);
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
            readable.Append('\\').Append(letter);
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
