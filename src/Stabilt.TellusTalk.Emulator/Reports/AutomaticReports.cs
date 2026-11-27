using Microsoft.Extensions.Options;
using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Reports;

/// <summary>
/// Sends each message's delivery report once its delay has passed, as TellusTalk reports a few seconds after a send:
/// delivered, or failed for a number the settings name. A tick starts its reports without waiting for them, so a
/// receiver that never answers holds up no other report, as K8 was seen to go on reporting beside one.
/// </summary>
internal sealed class AutomaticReports(
    MessageStore store,
    ReportSender sender,
    TimeProvider time,
    IOptions<EmulatorOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(options.Value.ReportDelaySeconds);
        var running = new List<Task>();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                running.RemoveAll(task => task.IsCompletedSuccessfully);
                running.AddRange(store
                    .TakeDueReports(delay)
                    .Select(message => Report(message, stoppingToken)));
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
        var status = options.Value.Fails(message.To.Address) ? ReportStatus.Failed : ReportStatus.Delivered;
        try
        {
            store.AddReports(message.Id, await sender.Send(message, status, stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Stopping: the report was cancelled with the host.
        }
    }
}
