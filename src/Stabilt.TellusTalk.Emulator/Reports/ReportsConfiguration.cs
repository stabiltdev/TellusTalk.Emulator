namespace Stabilt.TellusTalk.Emulator.Reports;

internal static class ReportsConfiguration
{
    public static IServiceCollection AddReports(this IServiceCollection services)
    {
        services.AddHttpClient(ReportSender.ClientName, client => client.Timeout = TimeSpan.FromSeconds(10));
        services
            .AddSingleton<ReportSender>()
            .AddSingleton<HeldBackReports>()
            .AddHostedService(provider => provider.GetRequiredService<HeldBackReports>())
            .AddHostedService<AutomaticReports>();

        return services;
    }
}
