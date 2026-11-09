namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>Failures a test armed through the read API; each answers one send in TellusTalk's place.</summary>
internal sealed class SendFailures
{
    private readonly Lock _lock = new();
    private int _status;
    private int _remaining;

    /// <summary><c>null</c> when every armed failure is spent or cleared.</summary>
    public ArmedFailures? Armed
    {
        get
        {
            lock (_lock)
            {
                return _remaining > 0 ? new ArmedFailures(_status, _remaining) : null;
            }
        }
    }

    /// <summary>Replaces whatever was armed, so a test sets what it needs without first reading what is left.</summary>
    public ArmedFailures Arm(int status, int count)
    {
        lock (_lock)
        {
            _status = status;
            _remaining = count;

            return new ArmedFailures(status, count);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _remaining = 0;
        }
    }

    /// <summary>The status of the failure spent, or <c>null</c> when none is armed.</summary>
    public int? TrySpend()
    {
        lock (_lock)
        {
            if (_remaining == 0)
            {
                return null;
            }

            _remaining--;

            return _status;
        }
    }
}

internal sealed record ArmedFailures(int Status, int Remaining);
