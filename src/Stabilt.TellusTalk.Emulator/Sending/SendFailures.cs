namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>
/// Failures a test armed through the read API, for any send or for sends to one number; each answers one send in
/// TellusTalk's place, in the order they were armed.
/// </summary>
internal sealed class SendFailures
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, List<ArmedFailure>> _byNumber = new(StringComparer.Ordinal);
    private List<ArmedFailure> _any = [];

    /// <summary>What is left for any send, in the order it will be spent.</summary>
    public IReadOnlyList<ArmedFailure> Armed()
    {
        lock (_lock)
        {
            return [.. _any];
        }
    }

    /// <summary>What is left for sends to <paramref name="number"/>, in the order it will be spent.</summary>
    public IReadOnlyList<ArmedFailure> Armed(string number)
    {
        lock (_lock)
        {
            return _byNumber.TryGetValue(number, out var failures) ? [.. failures] : [];
        }
    }

    /// <summary>Replaces what was armed for any send, so a test sets what it needs without first reading what is left.</summary>
    public void Arm(IReadOnlyList<ArmedFailure> failures)
    {
        lock (_lock)
        {
            _any = [.. failures];
        }
    }

    /// <summary>Replaces what was armed for sends to <paramref name="number"/>.</summary>
    public void Arm(string number, IReadOnlyList<ArmedFailure> failures)
    {
        lock (_lock)
        {
            _byNumber[number] = [.. failures];
        }
    }

    /// <summary>Clears every failure, for any send and for every number.</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _any = [];
            _byNumber.Clear();
        }
    }

    public void Clear(string number)
    {
        lock (_lock)
        {
            _byNumber.Remove(number);
        }
    }

    /// <summary>Spends the next failure armed for any send, or gives <c>null</c> when none is.</summary>
    public SendFailure? TrySpend()
    {
        lock (_lock)
        {
            return Spend(_any);
        }
    }

    /// <summary>
    /// Spends the next failure of the first of <paramref name="numbers"/> that has one. TellusTalk answers a send once,
    /// whatever its recipients, so a send spends one failure at most.
    /// </summary>
    public SendFailure? TrySpend(IEnumerable<string> numbers)
    {
        lock (_lock)
        {
            foreach (var number in numbers)
            {
                if (_byNumber.TryGetValue(number, out var failures) && Spend(failures) is { } failure)
                {
                    return failure;
                }
            }

            return null;
        }
    }

    private static SendFailure? Spend(List<ArmedFailure> failures)
    {
        if (failures.Count == 0)
        {
            return null;
        }

        var next = failures[0];
        if (next.Remaining == 1)
        {
            failures.RemoveAt(0);
        }
        else
        {
            failures[0] = next with { Remaining = next.Remaining - 1 };
        }

        return new SendFailure(next.Status, TimeSpan.FromSeconds(next.DelaySeconds));
    }
}

/// <summary>One entry of an order: a status for the next <see cref="Remaining"/> sends, each answered after a delay.</summary>
internal sealed record ArmedFailure(int Status, int Remaining, int DelaySeconds);

internal sealed record SendFailure(int Status, TimeSpan Delay);
