using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Stabilt.TellusTalk.Emulator.Messages;
using Stabilt.TellusTalk.Emulator.Reports;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Tests.Messages;

public sealed class MessageStoreTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 7, 22, 0, 0, TimeSpan.Zero));

    [Fact]
    public void The_newest_message_comes_first()
    {
        var store = Store();
        var first = Send(store, "Först");
        _time.Advance(TimeSpan.FromMinutes(1));
        var second = Send(store, "Sedan");

        store.List().Should().Equal(second, first);
    }

    [Fact]
    public void A_scheduled_message_is_listed_as_scheduled_until_it_falls_due_and_then_by_its_due_time()
    {
        var store = Store();
        var scheduled = Send(store, "Senare", scheduledSendTime: "2026-11-07T22:30:00Z");
        _time.Advance(TimeSpan.FromMinutes(10));
        var sent = Send(store, "Nu");

        store.List(MessageState.Scheduled).Should().Equal(scheduled);
        store.List(MessageState.Sent).Should().Equal(sent);

        _time.Advance(TimeSpan.FromMinutes(30));

        store.List(MessageState.Scheduled).Should().BeEmpty();
        store.List(MessageState.Sent).Should().Equal(scheduled, sent);
        store.Count(MessageState.Sent).Should().Be(2);
    }

    [Fact]
    public void Messages_filter_by_recipient()
    {
        var store = Store();
        var wanted = Send(store, "Hej", to: "sms:+46701740605");
        Send(store, "Hej", to: "sms:+46701740606");

        store.List(to: "+46701740605").Should().Equal(wanted);
    }

    [Fact]
    public void Messages_filter_by_a_search_over_number_text_and_sender_ignoring_case()
    {
        var store = Store();
        var welcome = Send(store, "Välkommen till tjänsten!");
        Send(store, "Din kod är 482913");

        store.List(search: "välkommen").Should().Equal(welcome);
        store.List(search: "VÄLKOMMEN").Should().Equal(welcome);
        store.List(search: "740605").Should().HaveCount(2);
    }

    [Fact]
    public void A_job_larger_than_the_retention_limit_leaves_the_newest_messages()
    {
        var store = Store(retentionLimit: 3);
        var request = Request("""{"to":["sms:+46701740601","sms:+46701740602","sms:+46701740603","sms:+46701740604","sms:+46701740605"],"text":"Hej"}""");

        store.Add(Factory().Create(request, "tester", "{}").Messages);

        store.List()
            .Select(message => message.To.Number)
            .Should().BeEquivalentTo(["+46701740603", "+46701740604", "+46701740605"]);
    }

    [Fact]
    public void A_message_is_found_removed_and_cleared_by_id()
    {
        var store = Store();
        var kept = Send(store, "Kvar");
        var removed = Send(store, "Borta");

        store.Find(kept.Id).Should().Be(kept);
        store.Remove(removed.Id).Should().BeTrue();
        store.Remove(removed.Id).Should().BeFalse();
        store.Find(removed.Id).Should().BeNull();

        store.Clear();

        store.List().Should().BeEmpty();
    }

    [Fact]
    public async Task Changes_announce_what_happened_to_the_store()
    {
        var store = Store();
        var cancellation = TestContext.Current.CancellationToken;
        await using var changes = store
            .Changes(cancellation)
            .GetAsyncEnumerator(cancellation);
        var next = changes.MoveNextAsync();

        var message = Send(store, "Hej");
        (await next).Should().BeTrue();
        changes.Current.Should().Be(new StoreChange("added"));

        store.Remove(message.Id);
        (await changes.MoveNextAsync()).Should().BeTrue();
        changes.Current.Should().Be(new StoreChange("removed"));

        store.Clear();
        (await changes.MoveNextAsync()).Should().BeTrue();
        changes.Current.Should().Be(new StoreChange("cleared"));
    }

    [Fact]
    public async Task A_scheduled_message_falling_due_is_announced_once()
    {
        var store = Store();
        using var listening = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Send(store, "Senare", scheduledSendTime: "2026-11-07T22:30:00Z");
        var changes = store
            .Changes(listening.Token)
            .GetAsyncEnumerator(listening.Token);
        var next = changes.MoveNextAsync();

        store.ReleaseDue();
        next.IsCompleted.Should().BeFalse();

        _time.Advance(TimeSpan.FromMinutes(31));
        store.ReleaseDue();
        (await next).Should().BeTrue();
        changes.Current.Should().Be(new StoreChange("due"));

        var after = changes.MoveNextAsync();
        store.ReleaseDue();
        after.IsCompleted.Should().BeFalse();

        await listening.CancelAsync();
        await FluentActions
            .Awaiting(async () => await after)
            .Should().ThrowAsync<OperationCanceledException>();
        await changes.DisposeAsync();
    }

    [Fact]
    public void A_report_is_handed_out_once_its_delay_has_passed_and_only_once()
    {
        var store = Store();
        var message = Send(store, "Hej", reportTo: "https://example.se/reports");
        Send(store, "Utan rapport");

        store.TakeDueReports(TimeSpan.FromSeconds(5)).Should().BeEmpty();
        _time.Advance(TimeSpan.FromSeconds(5));

        store.TakeDueReports(TimeSpan.FromSeconds(5)).Should().Equal(message);
        store.TakeDueReports(TimeSpan.FromSeconds(5)).Should().BeEmpty();
    }

    [Fact]
    public void A_message_due_at_the_end_of_time_is_never_due_for_a_report()
    {
        var store = Store();
        Send(store, "Aldrig", scheduledSendTime: "9999-12-31T23:59:59Z", reportTo: "https://example.se/reports");

        store.TakeDueReports(TimeSpan.FromSeconds(1)).Should().BeEmpty();
    }

    [Fact]
    public void Report_attempts_are_listed_oldest_first_whichever_was_recorded_first()
    {
        var store = Store();
        var message = Send(store, "Hej", reportTo: "https://example.se/reports");
        var earlier = Attempt(_time.GetUtcNow());
        var later = Attempt(_time.GetUtcNow().AddSeconds(5));

        store.AddReports(message.Id, [later]);
        store.AddReports(message.Id, [earlier]);

        store.Reports(message.Id).Should().Equal(earlier, later);
    }

    [Fact]
    public void Report_attempts_go_with_their_message()
    {
        var store = Store();
        var removed = Send(store, "Borta", reportTo: "https://example.se/reports");
        var cleared = Send(store, "Rensad", reportTo: "https://example.se/reports");
        store.AddReports(removed.Id, [Attempt(_time.GetUtcNow())]);
        store.AddReports(cleared.Id, [Attempt(_time.GetUtcNow())]);

        store.Remove(removed.Id);
        store.AddReports(removed.Id, [Attempt(_time.GetUtcNow())]);
        store.Clear();

        store.Reports(removed.Id).Should().BeEmpty();
        store.Reports(cleared.Id).Should().BeEmpty();
    }

    [Fact]
    public async Task A_recorded_report_is_announced()
    {
        var store = Store();
        var message = Send(store, "Hej", reportTo: "https://example.se/reports");
        var cancellation = TestContext.Current.CancellationToken;
        await using var changes = store
            .Changes(cancellation)
            .GetAsyncEnumerator(cancellation);
        var next = changes.MoveNextAsync();

        store.AddReports(message.Id, [Attempt(_time.GetUtcNow())]);

        (await next).Should().BeTrue();
        changes.Current.Should().Be(new StoreChange("reported"));
    }

    private static ReportAttempt Attempt(DateTimeOffset at) =>
        new(at, ReportStatus.Delivered, "https://example.se/reports", Sent: true, "200 OK");

    private MessageStore Store(int retentionLimit = 1000) =>
        new(_time, Options.Create(new EmulatorOptions { RetentionLimit = retentionLimit }));

    private MessageFactory Factory() =>
        new(_time, Options.Create(new EmulatorOptions()));

    private Message Send(
        MessageStore store,
        string text,
        string to = "sms:+46701740605",
        string? scheduledSendTime = null,
        string? reportTo = null)
    {
        var fields = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["to"] = to,
            ["text"] = text,
            ["scheduled_send_time"] = scheduledSendTime,
        };
        if (reportTo is not null)
        {
            fields["report_to"] = reportTo;
        }

        var json = JsonSerializer.Serialize(fields);
        var message = Factory()
            .Create(Request(json), "tester", json)
            .Messages
            .Single();
        store.Add([message]);

        return message;
    }

    private static SendRequest Request(string json)
    {
        using var document = JsonDocument.Parse(json);

        return SendRequestReader.Read(document.RootElement).Should().BeOfType<Accepted>().Subject.Request;
    }
}
