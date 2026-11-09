using System.Globalization;
using System.Text.RegularExpressions;

namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>
/// The links TellusTalk puts in an SMS text in place of <c>redirect=URL</c> and <c>--LINK_VIEW--</c>, in the form of the
/// specification's examples. The id is a placeholder of the examples' length, so the parts are counted on a text as long
/// as the phone's without the emulator writing a link that could lead to someone else's message.
/// </summary>
internal static partial class TrackingLinks
{
    private const string ViewToken = "--LINK_VIEW--";

    private const string ViewLink = "https://ebox.nu/z_xxxxxx";

    public static (string Text, IReadOnlyList<TrackingLink> Links) Shorten(string text)
    {
        var links = new List<TrackingLink>();
        var shortened = Token().Replace(text, match =>
        {
            if (string.Equals(match.Value, ViewToken, StringComparison.Ordinal))
            {
                links.Add(new TrackingLink(TrackingLinkKind.View, ViewLink, null));

                return ViewLink;
            }

            var redirects = links.Count(link => link.Kind == TrackingLinkKind.Redirect);
            var shown = string.Create(CultureInfo.InvariantCulture, $"https://ebox.nu/r_xxxxxx/{redirects}");
            links.Add(new TrackingLink(TrackingLinkKind.Redirect, shown, match.Groups["target"].Value));

            return shown;
        });

        return (shortened, links);
    }

    /// <summary>A redirect's address runs to the next whitespace, as nothing else marks where a URL in an SMS ends.</summary>
    [GeneratedRegex(@"redirect=(?<target>https?://\S+)|--LINK_VIEW--", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Token();
}

/// <summary>A link TellusTalk puts in an SMS text in place of one the request wrote.</summary>
/// <param name="Shown">The link as the phone shows it.</param>
/// <param name="Target">Where a redirect leads; <c>null</c> for the view link, which leads to the message's own page.</param>
internal sealed record TrackingLink(TrackingLinkKind Kind, string Shown, string? Target);

internal enum TrackingLinkKind
{
    Redirect,
    View,
}
