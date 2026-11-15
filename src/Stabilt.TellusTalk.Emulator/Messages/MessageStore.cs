using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Stabilt.TellusTalk.Emulator.Reports;
using Stabilt.TellusTalk.Emulator.Requests;

namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>Every caught message, in memory, newest last, and the changes made to them as they happen.</summary>
internal sealed class MessageStore(TimeProvider time, IOptions<EmulatorOptions> options)
{
    private readonly Lock _lock = new();
    private readonly List<Message> _messages = [];
    private readonly HashSet<string> _released = new(StringComparer.Ordinal);
    private readonly HashSet<string> _reportsTaken = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<ReportAttempt>> _reports = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<Guid, Channel<StoreChange>> _subscribers = new();

    /// <summary>K8 holds a send back as a duplicate of one "sent within 5 minutes".</summary>
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(5);

    /// <summary>Stores the messages, holding back each that duplicates one already stored, and returns them as stored.</summary>
    public IReadOnlyList<Message> Add(IReadOnlyList<Message> messages)
    {
        List<Message> stored = [];
        lock (_lock)
        {
            foreach (var message in messages)
            {
                // K8's duplicate key on another channel is unobserved, so only an SMS is held back.
                var original = options.Value.BlockDuplicates && message.To.Channel == MessageChannel.Sms
                    ? FindOriginal(message)
                    : null;
                var added = original is null ? message : message with { DuplicateOf = original.Id };
                _messages.Add(added);
                stored.Add(added);
            }

            var excess = _messages.Count - options.Value.RetentionLimit;
            if (excess > 0)
            {
                Forget(_messages
                    .Take(excess)
                    .Select(message => message.Id)
                    .ToList());
                _messages.RemoveRange(0, excess);
            }
        }

        Publish(StoreChange.Added);

        return stored;
    }

    /// <summary>
    /// Newest first by the time a message was shown: its due time if it was scheduled, else when it arrived. Messages
    /// at the same instant keep the order they arrived in, newest first.
    /// </summary>
    public IReadOnlyList<Message> List(MessageState? state = null, string? to = null, string? search = null)
    {
        var now = time.GetUtcNow();
        lock (_lock)
        {
            return _messages
                .Select((message, order) => (Message: message, Order: order))
                .Where(entry => state is null || entry.Message.StateAt(now) == state)
                .Where(entry => to is null || string.Equals(entry.Message.To.Address, to, StringComparison.Ordinal))
                .Where(entry => search is null || Matches(entry.Message, search))
                .OrderByDescending(entry => entry.Message.ShownAt)
                .ThenByDescending(entry => entry.Order)
                .Select(entry => entry.Message)
                .ToList();
        }
    }

    public int Count(MessageState state)
    {
        var now = time.GetUtcNow();
        lock (_lock)
        {
            return _messages.Count(message => message.StateAt(now) == state);
        }
    }

    public Message? Find(string id)
    {
        lock (_lock)
        {
            return _messages.Find(message => string.Equals(message.Id, id, StringComparison.Ordinal));
        }
    }

    public bool Remove(string id)
    {
        bool removed;
        lock (_lock)
        {
            removed = _messages.RemoveAll(message => string.Equals(message.Id, id, StringComparison.Ordinal)) > 0;
            Forget([id]);
        }

        if (removed)
        {
            Publish(StoreChange.Removed);
        }

        return removed;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
            _released.Clear();
            _reportsTaken.Clear();
            _reports.Clear();
        }

        Publish(StoreChange.Cleared);
    }

    /// <summary>
    /// The messages whose automatic report is due: they are SMS with a report address, were sent or fell due at
    /// least <paramref name="delay"/> ago, and have not been taken before. Each is handed out once.
    /// </summary>
    public IReadOnlyList<Message> TakeDueReports(TimeSpan delay)
    {
        var now = time.GetUtcNow();
        lock (_lock)
        {
            var due = _messages
                .Where(message => message.Sms is not null)
                .Where(message => message.DuplicateOf is null)
                .Where(message => message.Request.ReportTo.Count > 0)
                .Where(message => message.ShownAt <= now - delay)
                .Where(message => !_reportsTaken.Contains(message.Id))
                .ToList();

            due.ForEach(message => _reportsTaken.Add(message.Id));

            return due;
        }
    }

    public void AddReports(string id, IReadOnlyList<ReportAttempt> attempts)
    {
        lock (_lock)
        {
            if (_messages.Exists(message => string.Equals(message.Id, id, StringComparison.Ordinal)))
            {
                if (!_reports.TryGetValue(id, out var reports))
                {
                    reports = [];
                    _reports[id] = reports;
                }

                reports.AddRange(attempts);
            }
        }

        Publish(StoreChange.Reported);
    }

    public IReadOnlyList<ReportAttempt> Reports(string id)
    {
        lock (_lock)
        {
            return _reports.TryGetValue(id, out var reports) ? [.. reports.OrderBy(report => report.At)] : [];
        }
    }

    /// <summary>Announces once that scheduled messages have fallen due, so a page showing the lists can refresh.</summary>
    public void ReleaseDue()
    {
        var now = time.GetUtcNow();
        bool released;
        lock (_lock)
        {
            var due = _messages
                .Where(message => message.DuplicateOf is null)
                .Where(message => message.DueAt <= now)
                .Where(message => !_released.Contains(message.Id))
                .ToList();

            due.ForEach(message => _released.Add(message.Id));
            released = due.Count > 0;
        }

        if (released)
        {
            Publish(StoreChange.Due);
        }
    }

    /// <summary>The changes from the moment enumeration starts until it is cancelled.</summary>
    public async IAsyncEnumerable<StoreChange> Changes([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<StoreChange>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
        });
        _subscribers[id] = channel;

        try
        {
            await foreach (var change in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return change;
            }
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
        }
    }

    /// <summary>Drops what the store kept beside messages that are gone. Called under the lock.</summary>
    private void Forget(IReadOnlyCollection<string> ids)
    {
        foreach (var id in ids)
        {
            _released.Remove(id);
            _reportsTaken.Remove(id);
            _reports.Remove(id);
        }
    }

    /// <summary>Called under the lock. Held-back messages are no original, so a held-back send does not extend the window.</summary>
    private Message? FindOriginal(Message message) => _messages
        .Where(other => other.To.Channel == MessageChannel.Sms)
        .Where(other => other.DuplicateOf is null)
        .Where(other => (other.ReceivedAt - message.ReceivedAt).Duration() <= DuplicateWindow)
        .Where(other => string.Equals(other.UserName, message.UserName, StringComparison.Ordinal))
        .Where(other => string.Equals(other.To.Address, message.To.Address, StringComparison.Ordinal))
        .Where(other => string.Equals(other.Request.Subject ?? "", message.Request.Subject ?? "", StringComparison.Ordinal))
        .Where(other => string.Equals(other.Request.Text ?? "", message.Request.Text ?? "", StringComparison.Ordinal))
        .FirstOrDefault();

    private static bool Matches(Message message, string search) =>
        message.To.Address.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        message.Text.Contains(search, StringComparison.OrdinalIgnoreCase) ||
        message.Sms?.Sender.Display.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private void Publish(StoreChange change)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(change);
        }
    }
}
