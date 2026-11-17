# TellusTalk Emulator

A local stand-in for TellusTalk's K8 send API: it catches what an app sends, shows it on a web page, and serves it to
automated tests, so an app runs its real TellusTalk client against it with only the base address changed. .NET 11,
ASP.NET Core minimal APIs, Razor components rendered on the server, one container image.

## Read first

- [README.md](README.md): what it does, how it runs, and the read API.
- [docs/k8-behaviour.md](docs/k8-behaviour.md): what K8 was seen to do, with dates, and what it was not.
- [docs/decisions/README.md](docs/decisions/README.md): why things are the way they are.
- [docs/conventions.md](docs/conventions.md): how code and tests are written here.

## Never

- **Send an SMS onward.** Nothing the emulator catches reaches a phone, a mailbox or TellusTalk. Its only outgoing calls
  are delivery reports to the http(s) URLs a request names.
- **Be stricter or more lenient than TellusTalk.** Refuse only what TellusTalk was seen to refuse and what the emulator
  cannot record; what the specification forbids becomes a warning.
- **Present a reading of the specification as observed fact.** What rests on the document is listed in
  `docs/k8-behaviour.md`.
- **Pass for TellusTalk's own product.** The project is not affiliated with TellusTalk.
- **Borrow models from a TellusTalk client.** The emulator reads the specification on its own, so it cannot agree with
  a client's mistakes.
- **Write personal data or a TellusTalk customer's name** into code, tests, logs or documents. Test numbers come from
  PTS's range for fiction, `+46701740605` to `+46701740699`.

## Map

| Path | What |
|---|---|
| `src/Stabilt.TellusTalk.Emulator/Authentication` | TellusTalk's Basic authentication as `send/v1` takes it: `BasicAuthenticationHandler` and `BasicCredentials` |
| `src/Stabilt.TellusTalk.Emulator/Sending` | TellusTalk's send API: the endpoint and its filters, TellusTalk's headers, and the failures a test arms |
| `src/Stabilt.TellusTalk.Emulator/Sending/HttpResults` | Every answer to a send as a type of its own: `Refusals` and the `TextResult` kinds it makes, and `SendAccepted` |
| `src/Stabilt.TellusTalk.Emulator/Sending/Reading` | Reading a send request: the reader, the specification's rules, TellusTalk's sender refusals, and Python's rendering of a value in K8's texts |
| `src/Stabilt.TellusTalk.Emulator/Requests` | A caught send's request: the typed body, its recipients and their channels, report addresses, numbers, scheduled times and the warnings |
| `src/Stabilt.TellusTalk.Emulator/Senders` | The sender as the recipient's phone shows it, and the user's Sender setting and reply number |
| `src/Stabilt.TellusTalk.Emulator/Parts` | GSM 03.38 and UCS-2 part counting |
| `src/Stabilt.TellusTalk.Emulator/Messages` | The in-memory store with its duplicate rule and the queue of what it holds back, scheduled release, and the read API's message endpoints |
| `src/Stabilt.TellusTalk.Emulator/Reports` | Delivery reports: their form, sending them, sending them automatically or on request, and the blocked report of a held-back send, `HeldBackReports` |
| `src/Stabilt.TellusTalk.Emulator/Page` | The page's Razor components and endpoint; `wwwroot` holds its CSS, script and fonts |
| `src/Stabilt.TellusTalk.Emulator/Http` | `IEndpoint`, `MapEndpoints` and the read API's error body |
| `src/Stabilt.TellusTalk.Emulator/Ids` | `Guid.CreateVersion1` |
| `src/Stabilt.TellusTalk.Emulator.Aspire` | The Aspire package: `AddTellusTalkEmulator`, its settings and `WithReference`, in Aspire's own namespaces |
| `src/Stabilt.TellusTalk.Emulator.Testcontainers` | The Testcontainers package: `TellusTalkEmulatorBuilder`, its settings, and `TellusTalkEmulatorContainer.GetBaseAddress` |
| `src/Stabilt.TellusTalk.Emulator.Client` | The client package: `TellusTalkEmulatorClient`, `MessageQuery`, and the models of a caught message, `CaughtMessage` |
| `tests/Stabilt.TellusTalk.Emulator.Tests` | One folder per area |
| `tests/Stabilt.TellusTalk.Emulator.Aspire.Tests` | The Aspire package's app model, read without running it |
| `tests/Stabilt.TellusTalk.Emulator.Testcontainers.Tests` | The Testcontainers package, started in Docker from a locally built image |
| `tests/Stabilt.TellusTalk.Emulator.Client.Tests` | The client package against the emulator's host in memory |
| `tests/Shared` | Files linked into the test projects: the licence acceptance, and in `Support`, `EmulatorFactory` and `ReportReceiver` |
| `.github/workflows/ci.yml` | Build, format check, the image for the tests, and the tests; a `v*` tag publishes the image, then the packages |
| `.github/dependabot.yml` | Weekly updates to the packages, the SDK and the actions; `Aspire.Hosting` takes security updates only |

## Commands

| Task | Command |
|---|---|
| Build like CI | `dotnet build Stabilt.TellusTalk.Emulator.slnx -c Release` |
| Format check | `dotnet format Stabilt.TellusTalk.Emulator.slnx --verify-no-changes` |
| Test | `dotnet test --solution Stabilt.TellusTalk.Emulator.slnx` (Microsoft Testing Platform, through `global.json`) |
| Run | `dotnet run --project src/Stabilt.TellusTalk.Emulator` |
| Build the image | `dotnet publish src/Stabilt.TellusTalk.Emulator -c Release -r linux-x64 -t:PublishContainer -p:LocalRegistry=Docker` |
| Test the Testcontainers package | Build the image, then the tests with `TELLUSTALK_EMULATOR_TEST_IMAGE=stabiltdev/tellustalk-emulator:latest`; without it they are skipped |
| Pack the packages | `dotnet pack src/Stabilt.TellusTalk.Emulator.Aspire -c Release`, and the same for `src/Stabilt.TellusTalk.Emulator.Testcontainers` and `src/Stabilt.TellusTalk.Emulator.Client` |

## Traps

- Debug builds hide analyzer errors. Build `-c Release` before pushing.
- Time comes from `TimeProvider`; `DateTime.Now` and its kin are banned in `BannedSymbols.txt`.
- The report service's timer may start after a test has moved the fake clock. A test waiting for a report moves the
  clock a second at a time.
- The read API's JSON is a contract other repositories test against. `ReadApiTests` pins every value; change it
  deliberately and update the README with it. The client package's models read it, so a change to it changes them in
  the same change.
- The page must work without its script, as plain linked pages. The script swaps parts by their ids, so a part it swaps
  keeps its `id`.
- Analyzers can misread C# 14 extension blocks; suppress what they report there with a reason.
- Messages live in memory; a restart empties the lists.
- A test that sends the same text to the same number twice within five minutes meets a held-back message; give each send
  its own text, unless the test is about duplicates.
- A new setting goes into `EmulatorOptions`, the README's settings table, and both packages' builders.
- `Aspire.Hosting` stays on the oldest version without a known vulnerability; moving every package to its newest leaves
  it out.
