# Stabilt.TellusTalk.Emulator.Client

Reads what [TellusTalk Emulator](https://github.com/stabiltdev/TellusTalk.Emulator), a local stand-in for TellusTalk's
K8 send API, caught, through the emulator's own API, so a test asserts on typed properties instead of picking through
JSON. It is no TellusTalk client and calls no TellusTalk API. Not affiliated with TellusTalk.

With [Testcontainers](https://www.nuget.org/packages/Stabilt.TellusTalk.Emulator.Testcontainers):

```csharp
using var emulatorApi = new TellusTalkEmulatorClient(new Uri(tellusTalk.GetBaseAddress()));

await emulatorApi.DeleteMessagesAsync();

// … the code under test sends an SMS …

var sent = await emulatorApi.GetMessagesAsync(new MessageQuery
{
    State = MessageState.Sent,
    Channel = MessageChannel.Sms,
    To = "+46701740605",
});

var message = Assert.Single(sent);
Assert.Equal("Your order has shipped.", message.Text);
Assert.Equal("Acme", message.Sender!.Shown);
Assert.Equal("order-1001", message.Fields["echo"].GetString());
```

With [Aspire](https://www.nuget.org/packages/Stabilt.TellusTalk.Emulator.Aspire)'s testing host, from the emulator's
resource name:

```csharp
using var emulatorApi = new TellusTalkEmulatorClient(app.CreateHttpClient("tellustalk"));

await emulatorApi.DeleteMessagesAsync();

// … the code under test sends an SMS …

var sent = await emulatorApi.GetMessagesAsync(new MessageQuery { To = "+46701740605" });

var message = Assert.Single(sent);
Assert.Equal("Your order has shipped.", message.Text);
Assert.Equal("Acme", message.Sender!.Shown);
Assert.Equal("order-1001", message.Fields["echo"].GetString());
```

| Member | What it does |
|---|---|
| `new TellusTalkEmulatorClient(httpClient)` | Reads through an `HttpClient` whose `BaseAddress` is the emulator's, and never disposes it |
| `new TellusTalkEmulatorClient(baseAddress)` | Reads from the emulator at that address, through an `HttpClient` it disposes with itself |
| `GetMessagesAsync(query)` | The messages the emulator caught, newest first; without a query, all of them |
| `GetMessageAsync(id)` | One message, or `null` when the emulator has none with that id |
| `DeleteMessagesAsync()` | Removes every message, the held-back ones too; armed send failures stay |
| `MessageQuery.State` | `Sent`, `Scheduled` or `Blocked` |
| `MessageQuery.Channel` | `Sms`, `Email`, `Fax`, `Mms`, `Voice` or `Group` |
| `MessageQuery.To` | A message's `To` as given, on any channel: a number with its `+`, an e-mail address, a group |
| `MessageQuery.Search` | Searches the address, the text, an SMS's sender and another channel's subject |

A message is a `CaughtMessage` with every property the read API serves: its state and, for a blocked message, its
`BlockedReason`, `Duplicate` or `NumberNotNumeric`, the sender as the phone shows it, the parts, the report and reply
addresses, the warnings, the reports sent, and the request as it arrived, with its fields as JSON values. A status
other than success, and other than `404` for one message, throws `HttpRequestException`. A base address with a path of
its own needs a trailing `/`.

The package's version should match the emulator image's: a value it does not know, such as a channel added by a newer
image, fails the read.
