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

    [Fact]
    public void A_send_identical_to_one_received_within_five_minutes_is_held_back_as_its_duplicate()
    {
        var store = Store();
        var first = Send(store, "Your code is 482913");
        _time.Advance(TimeSpan.FromSeconds(280));
        var second = Send(store, "Your code is 482913");

        second.DuplicateOf.Should().Be(first.Id);
        second.StateAt(_time.GetUtcNow()).Should().Be(MessageState.Blocked);
        store.List(MessageState.Sent).Should().Equal(first);
        store.List(MessageState.Blocked).Should().Equal(second);
    }

    [Fact]
    public void A_send_identical_to_one_received_more_than_five_minutes_before_goes_out()
    {
        var store = Store();
        Send(store, "Your code is 482913");
        _time.Advance(TimeSpan.FromSeconds(320));
        var second = Send(store, "Your code is 482913");

        second.DuplicateOf.Should().BeNull();
    }

    [Fact]
    public void A_held_back_send_does_not_extend_the_window()
    {
        var store = Store();
        Send(store, "Hej");
        _time.Advance(TimeSpan.FromMinutes(3));
        Send(store, "Hej").DuplicateOf.Should().NotBeNull();
        _time.Advance(TimeSpan.FromSeconds(140));
        var third = Send(store, "Hej");

        third.DuplicateOf.Should().BeNull();
    }

    [Fact]
    public void Another_number_user_subject_or_text_is_no_duplicate()
    {
        var store = Store();
        Send(store, "Hej");
        _time.Advance(TimeSpan.FromMinutes(1));

        Send(store, "Hej", to: "sms:+46701740606").DuplicateOf.Should().BeNull();
        Send(store, "Hej", user: "other").DuplicateOf.Should().BeNull();
        Send(store, "Hej", subject: "Acme").DuplicateOf.Should().BeNull();
        Send(store, "hej").DuplicateOf.Should().BeNull();
        Send(store, "Hej ").DuplicateOf.Should().BeNull();
    }

    [Fact]
    public void The_header_and_a_number_written_with_spaces_do_not_tell_sends_apart()
    {
        var store = Store();
        var first = Send(store, "Hej");
        _time.Advance(TimeSpan.FromMinutes(1));

        Send(store, "Hej", header: "Header ").DuplicateOf.Should().Be(first.Id);
        Send(store, "Hej", to: "sms:+46 70 174 06 05").DuplicateOf.Should().Be(first.Id);
    }

    [Fact]
    public void An_empty_subject_is_the_same_as_none()
    {
        var store = Store();
        var first = Send(store, "Hej");

        Send(store, "Hej", subject: "").DuplicateOf.Should().Be(first.Id);
    }

    [Fact]
    public void One_number_twice_in_a_request_is_held_back_the_second_time()
    {
        var store = Store();
        var request = Request("""{"to":["sms:+46701740607","sms:+46701740607"],"text":"Hej"}""");

        var stored = store.Add(Factory().Create(request, "tester", "{}").Messages);

        stored[1].DuplicateOf.Should().Be(stored[0].Id);
    }

    [Fact]
    public void A_send_scheduled_ahead_counts_from_when_it_was_received()
    {
        var store = Store();
        var scheduled = Send(store, "Hej", scheduledSendTime: "2026-11-07T22:11:00Z");

        Send(store, "Hej").DuplicateOf.Should().Be(scheduled.Id);
    }

    [Fact]
    public async Task A_held_back_scheduled_send_stays_blocked_and_never_falls_due()
    {
        var store = Store();
        var first = Send(store, "Hej");
        var held = Send(store, "Hej", scheduledSendTime: "2026-11-07T22:10:00Z");
        using var listening = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var changes = store
            .Changes(listening.Token)
            .GetAsyncEnumerator(listening.Token);
        var next = changes.MoveNextAsync();
        _time.Advance(TimeSpan.FromMinutes(15));

        store.ReleaseDue();

        held.StateAt(_time.GetUtcNow()).Should().Be(MessageState.Blocked);
        store.List(MessageState.Sent).Should().Equal(first);
        next.IsCompleted.Should().BeFalse();

        await listening.CancelAsync();
        await changes.DisposeAsync();
    }

    [Fact]
    public void A_held_back_scheduled_send_is_listed_by_when_it_was_received()
    {
        var store = Store();
        Send(store, "Hej");
        _time.Advance(TimeSpan.FromMinutes(1));
        var scheduled = Send(store, "Hej", scheduledSendTime: "2026-11-07T22:30:00Z");
        _time.Advance(TimeSpan.FromMinutes(1));
        var immediate = Send(store, "Hej");

        scheduled.ShownAt.Should().Be(scheduled.ReceivedAt);
        store.List(MessageState.Blocked).Should().Equal(immediate, scheduled);
    }

    [Fact]
    public void A_send_identical_to_one_received_exactly_five_minutes_before_is_held_back()
    {
        var store = Store();
        var first = Send(store, "Hej");
        _time.Advance(TimeSpan.FromMinutes(5));

        Send(store, "Hej").DuplicateOf.Should().Be(first.Id);
    }

    [Fact]
    public void Of_two_sends_stored_in_the_opposite_order_the_later_stored_is_held_back()
    {
        var store = Store();
        var a = Create("Hej");
        _time.Advance(TimeSpan.FromSeconds(1));
        var b = Create("Hej");

        store.Add([b]);
        var stored = store.Add([a]).Single();

        stored.DuplicateOf.Should().Be(b.Id);
    }

    [Fact]
    public void A_duplicate_is_held_back_though_its_original_leaves_in_the_same_add()
    {
        var store = Store(retentionLimit: 1);
        var first = Send(store, "Hej");
        var second = Send(store, "Hej");

        second.DuplicateOf.Should().Be(first.Id);
        store.List().Should().Equal(second);
    }

    [Fact]
    public void Clearing_the_messages_forgets_what_could_hold_a_send_back()
    {
        var store = Store();
        Send(store, "Hej");
        store.Clear();

        Send(store, "Hej").DuplicateOf.Should().BeNull();
    }

    [Fact]
    public void With_holding_back_off_every_send_goes_out()
    {
        var store = Store(blockDuplicates: false);
        Send(store, "Hej").DuplicateOf.Should().BeNull();
        Send(store, "Hej").DuplicateOf.Should().BeNull();

        store.Count(MessageState.Blocked).Should().Be(0);
    }

    [Fact]
    public void A_held_back_message_is_not_handed_out_for_an_automatic_report()
    {
        var store = Store();
        var first = Send(store, "Hej", reportTo: "https://example.se/reports");
        Send(store, "Hej", reportTo: "https://example.se/reports");
        _time.Advance(TimeSpan.FromSeconds(2));

        store.TakeDueReports(TimeSpan.FromSeconds(1)).Should().Equal(first);
    }

    private static ReportAttempt Attempt(DateTimeOffset at) =>
        new(at, ReportStatus.Delivered, "https://example.se/reports", Sent: true, "200 OK");

    private MessageStore Store(int retentionLimit = 1000, bool blockDuplicates = true) =>
        new(_time, Options.Create(new EmulatorOptions { RetentionLimit = retentionLimit, BlockDuplicates = blockDuplicates }));

    private MessageFactory Factory() =>
        new(_time, Options.Create(new EmulatorOptions()));

    private Message Create(
        string text,
        string to = "sms:+46701740605",
        string? scheduledSendTime = null,
        string? reportTo = null,
        string? subject = null,
        string user = "tester",
        string? header = null)
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

        if (subject is not null)
        {
            fields["subject"] = subject;
        }

        if (header is not null)
        {
            fields["sms_header"] = header;
        }

        var json = JsonSerializer.Serialize(fields);

        return Factory()
            .Create(Request(json), user, json)
            .Messages
            .Single();
    }

    private Message Send(
        MessageStore store,
        string text,
        string to = "sms:+46701740605",
        string? scheduledSendTime = null,
        string? reportTo = null,
        string? subject = null,
        string user = "tester",
        string? header = null) =>
        store
            .Add([Create(text, to, scheduledSendTime, reportTo, subject, user, header)])
            .Single();

    private static SendRequest Request(string json)
    {
        using var document = JsonDocument.Parse(json);

        return SendRequestReader.Read(document.RootElement).Should().BeOfType<Accepted>().Subject.Request;
    }
}
