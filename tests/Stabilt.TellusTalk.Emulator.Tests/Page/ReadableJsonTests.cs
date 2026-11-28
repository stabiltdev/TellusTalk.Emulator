using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Page;

namespace Stabilt.TellusTalk.Emulator.Tests.Page;

/// <summary>The escapes in these tests are text in the JSON, not C# escapes, so they sit in raw and verbatim strings.</summary>
public sealed class ReadableJsonTests
{
    [Fact]
    public void An_object_is_written_a_member_a_line_and_indented_two_spaces_a_level()
    {
        var readable = ReadableJson.Write("""{"to":"sms:+46701740605","echo":{"ids":[1,2]}}""");

        readable.Should().Be(
            """
            {
              "to": "sms:+46701740605",
              "echo": {
                "ids": [
                  1,
                  2
                ]
              }
            }
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void An_empty_object_or_array_stays_on_one_line()
    {
        var readable = ReadableJson.Write("""{"echo":{},"to":[]}""");

        readable.Should().Be(
            """
            {
              "echo": {},
              "to": []
            }
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Numbers_and_literals_are_written_as_sent()
    {
        var readable = ReadableJson.Write("""{"a":1.0,"b":1e3,"c":12345678901234567890,"d":-0,"e":true,"f":false,"g":null}""");

        readable.Should().Be(
            """
            {
              "a": 1.0,
              "b": 1e3,
              "c": 12345678901234567890,
              "d": -0,
              "e": true,
              "f": false,
              "g": null
            }
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Members_keep_their_order_and_their_duplicates()
    {
        var readable = ReadableJson.Write("""{"text":"b","to":"x","text":"a"}""");

        readable.Should().Be(
            """
            {
              "text": "b",
              "to": "x",
              "text": "a"
            }
            """.ReplaceLineEndings("\n"));
    }

    [Theory]
    [InlineData(@"sms:\u002B46701740605", "sms:+46701740605")]
    [InlineData(@"H\u00E4lsningar", "Hälsningar")]
    [InlineData(@"Tack \uD83D\uDE0A", "Tack 😊")]
    [InlineData(@"https:\/\/example.se", "https://example.se")]
    [InlineData("Hälsningar", "Hälsningar")]
    public void A_string_writes_a_visible_character_as_itself(string sent, string readable)
    {
        ReadableJson.Write(Member(sent)).Should().Be(Readable(readable));
    }

    [Theory]
    [InlineData(@"a\""b", @"a\""b")]
    [InlineData(@"\u0022", @"\""")]
    [InlineData(@"\u005C", @"\\")]
    [InlineData(@"rad 1\u000Arad 2", @"rad 1\nrad 2")]
    [InlineData(@"a\tb", @"a\tb")]
    [InlineData(@"\u0001", @"\u0001")]
    [InlineData(@"\u007F", @"\u007F")]
    public void A_quote_a_backslash_or_a_control_character_stays_escaped(string sent, string readable)
    {
        ReadableJson.Write(Member(sent)).Should().Be(Readable(readable));
    }

    [Fact]
    public void A_control_character_sent_unescaped_is_written_escaped()
    {
        ReadableJson.Write(Member("a" + (char)0x85 + "b")).Should().Be(Readable(@"a\u0085b"));
    }

    [Theory]
    [InlineData(@"\uD800x")]
    [InlineData(@"\uDE0A")]
    public void Half_a_surrogate_pair_stays_the_escape_it_was_sent_as(string sent)
    {
        ReadableJson.Write(Member(sent)).Should().Be(Readable(sent));
    }

    [Fact]
    public void A_property_name_is_written_readable_too()
    {
        var readable = ReadableJson.Write("""{"sms_origin\u0061tor_text":"Probe"}""");

        readable.Should().Be(
            """
            {
              "sms_originator_text": "Probe"
            }
            """.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void On_one_line_members_and_items_are_parted_by_a_comma_and_a_space()
    {
        using var document = JsonDocument.Parse("""{"note":"H\u00E4lsningar","ids":[1,2],"empty":{},"none":[]}""");

        var readable = ReadableJson.WriteOnOneLine(document.RootElement);

        readable.Should().Be("""{"note": "Hälsningar", "ids": [1, 2], "empty": {}, "none": []}""");
    }

    /// <summary>A request whose one member, <c>text</c>, holds the given JSON text between its quotes.</summary>
    private static string Member(string content) => $$"""{"text":"{{content}}"}""";

    private static string Readable(string content) => $"{{\n  \"text\": \"{content}\"\n}}";
}
