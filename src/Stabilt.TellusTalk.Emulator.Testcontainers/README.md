# Stabilt.TellusTalk.Emulator.Testcontainers

Runs [TellusTalk Emulator](https://github.com/stabiltdev/TellusTalk.Emulator), a local stand-in for TellusTalk's K8
send API, from [Testcontainers for .NET](https://dotnet.testcontainers.org/). The emulator catches the SMS an app sends
and serves them to tests; nothing reaches a real phone. Not affiliated with TellusTalk.

```csharp
await using var tellusTalk = new TellusTalkEmulatorBuilder()
    .WithAccountSender("Acme")
    .Build();

await tellusTalk.StartAsync();

// The app under test, such as a WebApplicationFactory's host:
builder.UseSetting("TellusTalk:BaseUrl", tellusTalk.GetBaseAddress());
```

`StartAsync` returns once the emulator's health endpoint answers. `GetBaseAddress` is the address the app takes where it
would have `https://k8api.tellustalk.com/`, such as `http://127.0.0.1:32768/`; the app's TellusTalk client still needs
a user name and password, and the emulator takes any. The same address serves the emulator's page and the read API a
test asserts with, `api/v1/messages`.

| Method | Sets |
|---|---|
| `WithAccountSender(sender)` | The sender the TellusTalk account is configured with, shown when a request sets none |
| `WithFailingNumbers(numbers)` | The numbers whose delivery reports say the operator could not deliver |
| `WithReportDelay(delay)` | How long after a message is sent, or falls due, its delivery report goes out |
| `WithRetentionLimit(limit)` | How many messages the emulator keeps |

The package runs the emulator image of its own version, `ghcr.io/stabiltdev/tellustalk-emulator:<version>`;
`new TellusTalkEmulatorBuilder(image)` runs another.
