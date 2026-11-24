namespace Stabilt.TellusTalk.Emulator.Reports;

internal static class ReportsConfiguration
{
    /// <summary>
    /// How long K8 was seen to wait for a report's receiver before it failed the report, without a second attempt.
    /// </summary>
    private static readonly TimeSpan ReceiverTimeout = TimeSpan.FromSeconds(60);

    public static IServiceCollection AddReports(this IServiceCollection services)
    {
        services.AddHttpClient(ReportSender.ClientName, client => client.Timeout = ReceiverTimeout);
        services
            .AddSingleton<ReportSender>()
            .AddHostedService<HeldBackReports>()
            .AddHostedService<AutomaticReports>();

        return services;
    }
}
