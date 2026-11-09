namespace Stabilt.TellusTalk.Emulator.Sending;

/// <summary>Failures a test armed through the read API; each answers one send in TellusTalk's place.</summary>
internal sealed class SendFailures
{
    private readonly Lock _lock = new();
    private int _status;
    private int _delaySeconds;
    private int _remaining;

    /// <summary><c>null</c> when every armed failure is spent or cleared.</summary>
    public ArmedFailures? Armed
    {
        get
        {
            lock (_lock)
            {
                return _remaining > 0 ? new ArmedFailures(_status, _remaining, _delaySeconds) : null;
            }
        }
    }

    /// <summary>Replaces whatever was armed, so a test sets what it needs without first reading what is left.</summary>
    public ArmedFailures Arm(int status, int count, int delaySeconds)
    {
        lock (_lock)
        {
            _status = status;
            _remaining = count;
            _delaySeconds = delaySeconds;

            return new ArmedFailures(status, count, delaySeconds);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _remaining = 0;
        }
    }

    /// <summary>The failure spent, or <c>null</c> when none is armed.</summary>
    public SendFailure? TrySpend()
    {
        lock (_lock)
        {
            if (_remaining == 0)
            {
                return null;
            }

            _remaining--;

            return new SendFailure(_status, TimeSpan.FromSeconds(_delaySeconds));
        }
    }
}

internal sealed record ArmedFailures(int Status, int Remaining, int DelaySeconds);

internal sealed record SendFailure(int Status, TimeSpan Delay);
