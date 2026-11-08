namespace Stabilt.TellusTalk.Emulator.Messages;

/// <summary>Checks every second whether a scheduled message has fallen due.</summary>
internal sealed class ScheduledRelease(MessageStore store, TimeProvider time) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            store.ReleaseDue();
        }
    }
}
