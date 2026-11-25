using Stabilt.TellusTalk.Emulator.Senders;

namespace Stabilt.TellusTalk.Emulator;

internal static class EmulatorOptionsConfiguration
{
    public static IServiceCollection AddEmulatorOptions(this IServiceCollection services)
    {
        services
            .AddOptions<EmulatorOptions>()
            .BindConfiguration(EmulatorOptions.Section)
            .Validate(
                options => options.SmsSender is null || SmsSender.TryParse(options.SmsSender, out _),
                "Emulator__SmsSender must be replysms, text:<name>, number:<number> or subject.")
            .Validate(options => options.SmsMaxParts >= 1, "Emulator__SmsMaxParts must be 1 or more.")
            .ValidateOnStart();

        return services;
    }
}
