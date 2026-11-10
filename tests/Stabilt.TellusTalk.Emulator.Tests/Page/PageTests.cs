using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Stabilt.TellusTalk.Emulator.Tests.Support;

namespace Stabilt.TellusTalk.Emulator.Tests.Page;

public sealed class PageTests
{
    private const string Welcome =
        "Välkommen till kliniken! Vi har tagit emot din intresseanmälan och en av våra koordinatorer ringer dig inom två " +
        "arbetsdagar. Logga in med e-post för att följa ärendet och se vilka dokument vi behöver från dig.";

    [Fact]
    public async Task The_page_is_titled_and_counts_each_tab()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        await Send(emulator, """{"to":"sms:+46701740606","text":"Hej"}""");

        using var page = await Page(emulator);

        page.Title.Should().Be("TellusTalk Emulator");
        page.QuerySelector("#tabs a[data-tab='sent'] .count")!.TextContent.Should().Be("2");
        page.QuerySelector("#tabs a[data-tab='scheduled'] .count")!.TextContent.Should().Be("0");
    }

    [Fact]
    public async Task A_row_shows_the_recipient_the_sender_and_the_start_of_the_text_and_links_to_the_message()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, Json(new { to = "sms:+46701740605", text = Welcome, sms_originator_source = "text", sms_originator_text = "Probe" }));

        using var page = await Page(emulator);

        var row = page.QuerySelector("#list a.tt-row")!;
        row.GetAttribute("href").Should().Be($"/?tab=sent&id={id}");
        row.GetAttribute("data-id").Should().Be(id);
        row.QuerySelector(".num")!.TextContent.Should().Be("+46701740605");
        row.QuerySelector(".from")!.TextContent.Should().Be("Probe");
        row.QuerySelector(".prev")!.TextContent.Should().StartWith("Välkommen till kliniken!");
    }

    [Fact]
    public async Task The_detail_shows_the_text_in_its_parts_with_the_legend_and_the_sender()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, Json(new { to = "sms:+46701740605", text = Welcome, sms_originator_source = "text", sms_originator_text = "Probe" }));

        using var page = await Page(emulator, $"?id={id}");

        var detail = page.QuerySelector("#detail")!;
        detail.QuerySelector(".tt-bubble mark")!.TextContent.Should().StartWith("följa ärendet");
        detail.QuerySelector(".tt-bubble")!.TextContent.Should().Be(Welcome);
        var legend = detail.QuerySelector(".legend")!.TextContent;
        legend.Should().Contain("2 parts of at most 8, GSM characters").And.Contain("209 characters, 97 left in part 2");
        detail.TextContent.Should().Contain("From Probe, shown as a name.");
    }

    [Fact]
    public async Task The_legend_and_the_sender_line_follow_the_message()
    {
        await using var emulator = new EmulatorFactory();
        var single = await Send(emulator, """{"to":"sms:+46701740605","text":"Tack 😊","sms_originator_source":"number","sms_originator_number":"+46701740699"}""");

        using var page = await Page(emulator, $"?id={single}");

        var detail = page.QuerySelector("#detail")!;
        detail.QuerySelector(".legend")!.TextContent.Should().Contain("1 part of at most 8, Unicode characters");
        detail.TextContent.Should().Contain("From +46701740699, shown as a number.");
    }

    [Fact]
    public async Task A_message_without_a_sender_says_the_accounts_sender_is_shown()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        using var page = await Page(emulator, $"?id={id}");

        page.QuerySelector("#detail")!.TextContent.Should().Contain("The request set no sender, so the account's sender is shown.");
    }

    [Fact]
    public async Task A_message_with_warnings_is_marked_in_the_list_and_lists_them_in_the_detail()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46 70 174 06 05","text":"Hej","sms_orginator_text":"Probe"}""");

        using var page = await Page(emulator, $"?id={id}");

        page.QuerySelector("#list a.tt-row.warned").Should().NotBeNull();
        page.QuerySelectorAll("#detail .tt-warnings li")
            .Select(item => item.TextContent)
            .Should().SatisfyRespectively(
                first => first.Should().Contain("TellusTalk removes them"),
                second => second.Should().Contain("TellusTalk accepts it and ignores it"));
    }

    [Fact]
    public async Task A_report_address_with_settings_shows_the_url_and_masks_header_values()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, Json(new
        {
            to = "sms:+46701740605",
            text = "Hej",
            report_to = """https:{"headers":{"Authorization":"Basic dGVzdDp0ZXN0"}}://example.se/reports""",
        }));

        using var page = await Page(emulator, $"?id={id}");

        var detail = page.QuerySelector("#detail")!.TextContent;
        detail.Should().Contain("https://example.se/reports").And.Contain("Authorization: ••••••");
        page.QuerySelector("#detail dl")!.TextContent.Should().NotContain("dGVzdDp0ZXN0");
    }

    [Fact]
    public async Task A_report_address_and_each_of_its_settings_read_as_words_of_their_own()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, Json(new
        {
            to = "sms:+46701740605",
            text = "Hej",
            report_to = """https:{"content_type":"application/xml","headers":{"Authorization":"Basic dGVzdDp0ZXN0"}}://example.se/reports""",
        }));

        using var page = await Page(emulator, $"?id={id}");

        Collapsed(page.QuerySelector("#detail .tt-addresses li")!.TextContent)
            .Should().Be("https://example.se/reports as application/xml Authorization: ••••••");
    }

    [Fact]
    public async Task A_message_with_a_report_address_offers_a_delivered_and_a_failed_report()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":"https://example.se/reports"}""");

        using var page = await Page(emulator, $"?id={id}");

        var reports = page.QuerySelector("#detail .tt-reports")!;
        reports.TextContent.Should().Contain("None yet.");
        reports.QuerySelectorAll("button[data-report]")
            .Select(button => (button.GetAttribute("data-report"), button.GetAttribute("data-id")))
            .Should().Equal(("delivered", id), ("failed", id));
    }

    [Fact]
    public async Task The_detail_lists_each_report_attempt_with_its_status_target_and_outcome()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej","report_to":["https://example.se/reports","email:ops@example.se"]}""");
        using var client = emulator.CreateClient();
        using var report = await client.PostAsJsonAsync(
            new Uri($"/api/v1/messages/{id}/reports", UriKind.Relative),
            new { status = "failed" },
            TestContext.Current.CancellationToken);
        report.StatusCode.Should().Be(HttpStatusCode.OK);

        using var page = await Page(emulator, $"?id={id}");

        var attempts = page.QuerySelectorAll("#detail .tt-reports li");
        attempts.Should().HaveCount(2);
        attempts[0].TextContent.Should().Contain("Failed").And.Contain("https://example.se/reports").And.Contain("200 OK");
        attempts[0].QuerySelector("time")!.GetAttribute("datetime").Should().Be("2026-11-07T22:00:00Z");
        attempts[1].TextContent.Should().Contain("ops@example.se").And.Contain("Not sent: the emulator sends no e-mail.");
        attempts[1].ClassList.Should().Contain("unsent");
    }

    [Fact]
    public async Task A_message_without_a_report_address_shows_no_reports()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        using var page = await Page(emulator, $"?id={id}");

        page.QuerySelector("#detail .tt-reports").Should().BeNull();
    }

    [Fact]
    public async Task The_detail_shows_flash_validity_billing_id_and_echo_as_sent()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej","sms_flash":true,"sms_validity_time":3600,"billing_id":"team1","echo":{"probe":"1"}}""");

        using var page = await Page(emulator, $"?id={id}");

        Details(page).Should().Contain(
        [
            KeyValuePair.Create("Flash", "true"),
            KeyValuePair.Create("Valid for", "3600 seconds"),
            KeyValuePair.Create("Billing id", "team1"),
            KeyValuePair.Create("Echo", """{"probe":"1"}"""),
        ]);
    }

    [Fact]
    public async Task The_detail_shows_the_operators_defaults_for_what_a_request_leaves_out()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        using var page = await Page(emulator, $"?id={id}");

        var details = Details(page);
        details.Should().Contain(KeyValuePair.Create("Flash", "No")).And.Contain(KeyValuePair.Create("Valid for", "Operator default"));
        details.Should().NotContainKeys("Billing id", "Echo");
    }

    [Fact]
    public async Task The_detail_names_the_recipients_on_other_channels_the_emulator_did_not_catch()
    {
        await using var emulator = new EmulatorFactory();
        var smsOnly = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");
        emulator.Time.Advance(TimeSpan.FromMinutes(1));
        await Send(emulator, """{"to":["sms:+46701740605","email:test.user@example.se","fax:+46701740606"],"text":"Hej"}""");

        using var smsOnlyPage = await Page(emulator, $"?id={smsOnly}");
        using var mixedPage = await Page(emulator);

        Details(smsOnlyPage).Should().NotContainKey("Not caught");
        Details(mixedPage).Should().Contain(KeyValuePair.Create("Not caught", "email:test.user@example.se, fax:+46701740606"));
    }

    [Fact]
    public async Task The_detail_lists_each_tracking_link_with_where_it_leads_and_the_phone_links_nowhere()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Svara här: redirect=https://example.se/quiz eller läs --LINK_VIEW--"}""");

        using var page = await Page(emulator, $"?id={id}");

        page.QuerySelector("#detail .tt-bubble")!.TextContent.Should().Be("Svara här: https://ebox.nu/r_xxxxxx/0 eller läs https://ebox.nu/z_xxxxxx");
        page.QuerySelectorAll("#detail .tt-bubble a").Should().BeEmpty();
        page.QuerySelectorAll("#detail .tt-links li")
            .Select(item => Collapsed(item.TextContent))
            .Should().Equal(
                "https://ebox.nu/r_xxxxxx/0 leads to https://example.se/quiz",
                "https://ebox.nu/z_xxxxxx leads to the message's own page");
    }

    [Fact]
    public async Task A_message_without_tracking_links_lists_none()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        using var page = await Page(emulator, $"?id={id}");

        Details(page).Should().NotContainKey("Links");
    }

    [Fact]
    public async Task Markup_in_a_text_is_shown_as_text_and_never_interpreted()
    {
        await using var emulator = new EmulatorFactory();
        const string text = "<b>bold</b><script>alert(1)</script>";
        var id = await Send(emulator, Json(new { to = "sms:+46701740605", text }));

        using var page = await Page(emulator, $"?id={id}");

        page.QuerySelector("#detail .tt-bubble")!.TextContent.Should().Be(text);
        page.QuerySelector("#list .prev")!.TextContent.Should().Be(text);
        page.QuerySelectorAll("#list b, #list script, #detail .tt-bubble b, #detail .tt-bubble script").Should().BeEmpty();
    }

    [Fact]
    public async Task Markup_in_a_later_part_is_shown_as_text_inside_its_part_mark()
    {
        await using var emulator = new EmulatorFactory();
        var text = new string('a', 150) + " <b>bold</b><script>alert(1)</script>";
        var id = await Send(emulator, Json(new { to = "sms:+46701740605", text }));

        using var page = await Page(emulator, $"?id={id}");

        var bubble = page.QuerySelector("#detail .tt-bubble")!;
        bubble.TextContent.Should().Be(text);
        bubble.QuerySelector("mark")!.TextContent.Should().Contain("<script>");
        bubble.QuerySelectorAll("b, script").Should().BeEmpty();
    }

    [Fact]
    public async Task The_phone_leaves_the_SMS_text_readable_to_assistive_technology()
    {
        await using var emulator = new EmulatorFactory();
        var id = await Send(emulator, """{"to":"sms:+46701740605","text":"Hej"}""");

        using var page = await Page(emulator, $"?id={id}");

        page.QuerySelector("#detail .tt-phone")!.GetAttribute("role").Should().NotBe("img");
    }

    [Fact]
    public async Task The_scheduled_tab_lists_only_scheduled_messages_with_their_due_time()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Nu"}""");
        await Send(emulator, """{"to":"sms:+46701740605","text":"Senare","scheduled_send_time":"2026-11-07T23:00:00Z"}""");

        using var page = await Page(emulator, "?tab=scheduled");

        var row = page.QuerySelectorAll("#list a.tt-row").Should().ContainSingle().Subject;
        row.QuerySelector(".prev")!.TextContent.Should().Be("Senare");
        row.TextContent.Should().Contain("Due");
        row.QuerySelector("time")!.GetAttribute("datetime").Should().Be("2026-11-07T23:00:00Z");
    }

    [Fact]
    public async Task A_search_filters_the_list()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, Json(new { to = "sms:+46701740605", text = Welcome }));
        await Send(emulator, """{"to":"sms:+46701740605","text":"Din kod är 482913"}""");

        using var page = await Page(emulator, "?q=v%C3%A4lkommen");

        page.QuerySelectorAll("#list a.tt-row").Should().ContainSingle();
        page.QuerySelector("form#search input[name='q']")!.GetAttribute("value").Should().Be("välkommen");
    }

    [Fact]
    public async Task An_empty_inbox_says_how_to_fill_it()
    {
        await using var emulator = new EmulatorFactory();

        using var page = await Page(emulator);

        page.QuerySelector("#detail")!.TextContent.Trim().Should().Be("No messages yet. Send one to POST /send/v1 and it shows up here.");
    }

    [Fact]
    public async Task An_empty_scheduled_tab_says_nothing_is_scheduled()
    {
        await using var emulator = new EmulatorFactory();
        await Send(emulator, """{"to":"sms:+46701740605","text":"Nu"}""");

        using var page = await Page(emulator, "?tab=scheduled");

        page.QuerySelector("#detail")!.TextContent.Trim().Should().Be("Nothing is scheduled.");
    }

    private static async Task<IHtmlDocument> Page(EmulatorFactory emulator, string query = "")
    {
        using var client = emulator.CreateClient();
        using var response = await client.GetAsync(new Uri("/" + query, UriKind.Relative), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        return await new HtmlParser().ParseDocumentAsync(html, TestContext.Current.CancellationToken);
    }

    private static async Task<string> Send(EmulatorFactory emulator, string json)
    {
        using var response = await emulator.Send(json);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var answer = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        return answer.RootElement.GetProperty("job_id").GetString()!;
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);

    /// <summary>Text with its runs of whitespace made single spaces, as a browser shows the markup's indentation.</summary>
    private static string Collapsed(string text) => string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>The detail's list of terms, each with the text of the description that follows it.</summary>
    private static Dictionary<string, string> Details(IHtmlDocument page) => page
        .QuerySelectorAll("#detail dl dt")
        .ToDictionary(term => term.TextContent.Trim(), term => term.NextElementSibling!.TextContent.Trim(), StringComparer.Ordinal);
}
