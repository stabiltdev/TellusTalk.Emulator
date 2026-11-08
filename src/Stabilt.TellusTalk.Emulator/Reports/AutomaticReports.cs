using Microsoft.Extensions.Options;
using Stabilt.TellusTalk.Emulator.Messages;

namespace Stabilt.TellusTalk.Emulator.Reports;

/// <summary>
/// Sends each message's delivery report once its delay has passed, as TellusTalk reports a few seconds after a send:
/// delivered, or failed for a number the settings name. The messages due at a tick are reported side by side, so a
/// receiver that never answers holds up no other message for longer than the client's timeout.
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
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await Task.WhenAll(store
                .TakeDueReports(delay)
                .Select(message => Report(message, stoppingToken)));
        }
    }

    private async Task Report(Message message, CancellationToken stoppingToken)
    {
        var status = options.Value.Fails(message.To.Number) ? ReportStatus.Failed : ReportStatus.Delivered;
        store.AddReports(message.Id, await sender.Send(message, status, stoppingToken));
    }
}
