using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Reports;

/// <summary>
/// Reports a held-back send at once, as K8 posts its blocked report within half a second. The timer behind
/// <see cref="AutomaticReports"/> works from a due time and may start after a test has moved the clock, so this reads
/// each message from the store as it is held back. Each report is started as it arrives, so a receiver that never
/// answers holds up no other.
/// </summary>
internal sealed class HeldBackReports(MessageStore store, ReportSender sender) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var running = new List<Task>();
        var withReport = store
            .HeldBack(stoppingToken)
            .Where(message => message.Request.ReportTo.Count > 0);

        try
        {
            await foreach (var message in withReport)
            {
                running.RemoveAll(task => task.IsCompletedSuccessfully);
                running.Add(Report(message, stoppingToken));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping; the reports still in flight are cancelled with the same token.
        }
        finally
        {
            await Task.WhenAll(running);
        }
    }

    private async Task Report(Message message, CancellationToken stoppingToken)
    {
        try
        {
            store.AddReports(message.Id, await sender.Send(message, ReportStatus.Blocked, stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping: the report was cancelled with the host.
        }
    }
}
