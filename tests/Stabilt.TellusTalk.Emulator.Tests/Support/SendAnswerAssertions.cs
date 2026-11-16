using System.Globalization;

namespace Stabilt.TellusTalk.Emulator.Tests.Support;

/// <summary>What every answer to a send carries, whatever its status.</summary>
internal static class SendAnswerAssertions
{
    private const string Uuid1 = "^[0-9a-f]{12}1[0-9a-f]{3}[89ab][0-9a-f]{15}$";

    /// <summary>
    /// K8's headers, and the length of the body. The length is read unvalidated: asked for its parsed value, HttpClient
    /// computes one from the buffered body, so a missing header would go unnoticed.
    /// </summary>
    public static async Task ShouldCarryTellusTalksHeadersAndAContentLength(this HttpResponseMessage response)
    {
        Header(response, "X-Request-Id").Should().MatchRegex(Uuid1);
        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "Strict-Transport-Security").Should().Be("max-age=31536000; includeSubDomains");
        Header(response, "Cross-Origin-Opener-Policy").Should().Be("same-origin");
        Header(response, "Referrer-Policy").Should().Be("strict-origin-when-cross-origin");
        Header(response, "Cross-Origin-Resource-Policy").Should().Be("same-origin");
        Header(response, "Permissions-Policy").Should().Be("geolocation=(), camera=(), microphone=()");

        var hasLength = response.Content.Headers.NonValidated.TryGetValues("Content-Length", out var length);
        hasLength.Should().BeTrue("the emulator gives every answer its length");
        var body = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        length.ToString().Should().Be(body.Length.ToString(CultureInfo.InvariantCulture));
    }

    private static string Header(HttpResponseMessage response, string name) => response.Headers.GetValues(name).Single();
}
