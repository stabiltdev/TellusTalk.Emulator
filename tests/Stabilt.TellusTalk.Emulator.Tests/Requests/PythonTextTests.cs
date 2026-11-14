using System.Text.Json;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Requests;

public sealed class PythonTextTests
{
    [Theory]
    [InlineData("null", "None")]
    [InlineData("true", "True")]
    [InlineData("false", "False")]
    [InlineData("123", "123")]
    [InlineData("46701740699", "46701740699")]
    [InlineData("1.50", "1.5")]
    [InlineData("1e3", "1000.0")]
    [InlineData("1e16", "1e+16")]
    [InlineData("0.0001", "0.0001")]
    [InlineData("0.00001", "1e-05")]
    [InlineData("-0.0", "-0.0")]
    [InlineData("""["Acme", 1, null]""", "['Acme', 1, None]")]
    [InlineData("""{"name": "Acme", "on": true}""", "{'name': 'Acme', 'on': True}")]
    [InlineData("""["it's", "Å"]""", """["it's", 'Å']""")]
    [InlineData("""["a\nb"]""", """['a\nb']""")]
    [InlineData("[]", "[]")]
    [InlineData("{}", "{}")]
    public void A_value_prints_as_Pythons_str_prints_it(string json, string expected)
    {
        using var document = JsonDocument.Parse(json);

        PythonText.Of(document.RootElement).Should().Be(expected);
    }
}
