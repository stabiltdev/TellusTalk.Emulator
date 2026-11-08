# Stabilt.TellusTalk.Emulator.Aspire

Runs [TellusTalk Emulator](https://github.com/stabiltdev/TellusTalk.Emulator), a local stand-in for TellusTalk's K8
send API, from an Aspire AppHost. The emulator catches the SMS an app sends, shows them on a page, and serves them to
tests; nothing reaches a real phone. Not affiliated with TellusTalk.

```csharp
var tellusTalk = builder
    .AddTellusTalkEmulator("tellustalk")
    .WithAccountSender("Clinic");

builder
    .AddProject<Projects.Api>("api")
    .WithReference(tellusTalk)
    .WaitFor(tellusTalk);
```

`WithReference` gives the app the emulator's address as `TellusTalk__BaseUrl`, the setting `TellusTalk:BaseUrl`; pass
another name as its second argument if the app reads another setting. The dashboard links to the emulator's page.

| Method | Sets |
|---|---|
| `WithAccountSender(sender)` | The sender the TellusTalk account is configured with, shown when a request sets none |
| `WithFailingNumbers(numbers)` | The numbers whose delivery reports say the operator could not deliver |
| `WithReportDelay(delay)` | How long after a message is sent, or falls due, its delivery report goes out |
| `WithRetentionLimit(limit)` | How many messages the emulator keeps |

The package runs the emulator image of its own version, `ghcr.io/stabiltdev/tellustalk-emulator:<version>`;
`WithImageTag` picks another.
