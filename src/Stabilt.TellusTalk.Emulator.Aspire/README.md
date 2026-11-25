# Stabilt.TellusTalk.Emulator.Aspire

Runs [TellusTalk Emulator](https://github.com/stabiltdev/TellusTalk.Emulator), a local stand-in for TellusTalk's K8
send API, from an Aspire AppHost. The emulator catches what an app sends, shows it on a page, and serves it to tests;
nothing reaches a real phone or mailbox. Not affiliated with TellusTalk.

```csharp
var tellusTalk = builder
    .AddTellusTalkEmulator("tellustalk")
    .WithSmsSender(TellusTalkSmsSender.Text("Acme"));

builder
    .AddProject<Projects.Api>("api")
    .WithReference(tellusTalk)
    .WaitFor(tellusTalk);
```

`WithReference` gives the app the emulator's address as `TellusTalk__BaseUrl`, the setting `TellusTalk:BaseUrl`; pass
another name as its second argument if the app reads another setting. The app's own configuration still gives its
TellusTalk client a user name and password; the emulator takes any. The dashboard links to the emulator's page.

| Method | Sets |
|---|---|
| `WithSmsSender(sender)` | The user's Sender setting, shown when a request sets none: `TellusTalkSmsSender.Text(name)`, `.Number(number)`, `.ReplySms` or `.Subject` |
| `WithReplyNumber(number)` | The number TellusTalk assigns for replies, shown for `replysms` |
| `WithSmsMaxParts(maxParts)` | The user's Max number of parts, which cuts a text sent without `sms_max_parts`; 8 by default |
| `WithAccountSender(sender)` | Obsolete; use `WithSmsSender` |
| `WithFailingNumbers(numbers)` | Adds numbers whose delivery reports say the operator could not deliver; each call adds to the earlier ones |
| `WithReportDelay(delay)` | How long after a message is sent, or falls due, its delivery report goes out |
| `WithRetentionLimit(limit)` | How many messages the emulator keeps |
| `WithDuplicateBlocking(enabled)` | Whether an SMS that duplicates one from the five minutes before is held back, as TellusTalk does; on by default |

The package runs the emulator image of its own version, `ghcr.io/stabiltdev/tellustalk-emulator:<version>`;
`WithImageTag` picks another.
