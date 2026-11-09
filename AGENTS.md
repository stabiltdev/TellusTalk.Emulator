# TellusTalk Emulator

A local stand-in for TellusTalk's K8 send API: it catches the SMS an app sends, shows them on a web page, and serves them
to automated tests, so an app runs its real TellusTalk client against it with only the base address changed. .NET 10,
ASP.NET Core minimal APIs, Razor components rendered on the server, one container image.

## Read first

- [README.md](README.md): what it does, how it runs, the read API, and the behaviour that follows the specification
  rather than observation.
- [docs/decisions/README.md](docs/decisions/README.md): why things are the way they are.
- [docs/conventions.md](docs/conventions.md): how code and tests are written here.

## Never

- **Send an SMS onward.** Nothing the emulator catches reaches a phone, a mailbox or TellusTalk. Its only outgoing calls
  are delivery reports to the http(s) URLs a request names.
- **Be stricter or more lenient than TellusTalk.** Refuse only what TellusTalk was seen to refuse and what the emulator
  cannot record; what the specification forbids becomes a warning.
- **Present a reading of the specification as observed fact.** What rests on the document is listed in the README.
- **Pass for TellusTalk's own product.** The project is not affiliated with TellusTalk.
- **Borrow models from a TellusTalk client.** The emulator reads the specification on its own, so it cannot agree with
  a client's mistakes.
- **Write personal data or a TellusTalk customer's name** into code, tests, logs or documents. Test numbers come from
  PTS's range for fiction, `+46701740605` to `+46701740699`.

## Map

| Path | What |
|---|---|
| `src/Stabilt.TellusTalk.Emulator/Sending` | TellusTalk's send API: the endpoint, Basic authentication, refusals, TellusTalk's headers, and the failures a test arms |
| `src/Stabilt.TellusTalk.Emulator/Requests` | Reading a send request: the typed body, the reader, the specification's rules and the warnings |
| `src/Stabilt.TellusTalk.Emulator/Senders` | The sender as the recipient's phone shows it |
| `src/Stabilt.TellusTalk.Emulator/Parts` | GSM 03.38 and UCS-2 part counting |
| `src/Stabilt.TellusTalk.Emulator/Messages` | The in-memory store, scheduled release, and the read API's message endpoints |
| `src/Stabilt.TellusTalk.Emulator/Reports` | Delivery reports: their form, sending them, and sending them automatically or on request |
| `src/Stabilt.TellusTalk.Emulator/Page` | The page's Razor components and endpoint; `wwwroot` holds its CSS, script and fonts |
| `src/Stabilt.TellusTalk.Emulator/Http` | `IEndpoint`, `MapEndpoints` and the read API's error body |
| `src/Stabilt.TellusTalk.Emulator/Ids` | `Guid.CreateVersion1` |
| `src/Stabilt.TellusTalk.Emulator.Aspire` | The Aspire package: `AddTellusTalkEmulator`, its settings and `WithReference`, in Aspire's own namespaces |
| `tests/Stabilt.TellusTalk.Emulator.Tests` | One folder per area; `Support` holds `EmulatorFactory` and `ReportReceiver` |
| `tests/Stabilt.TellusTalk.Emulator.Aspire.Tests` | The Aspire package's app model, read without running it |
| `.github/workflows/ci.yml` | Build, format check and tests; a `v*` tag publishes the image, then the Aspire package |

## Commands

| Task | Command |
|---|---|
| Build like CI | `dotnet build Stabilt.TellusTalk.Emulator.slnx -c Release` |
| Format check | `dotnet format Stabilt.TellusTalk.Emulator.slnx --verify-no-changes` |
| Test | `dotnet test --solution Stabilt.TellusTalk.Emulator.slnx` (Microsoft Testing Platform, through `global.json`) |
| Run | `dotnet run --project src/Stabilt.TellusTalk.Emulator` |
| Build the image | `dotnet publish src/Stabilt.TellusTalk.Emulator -c Release -r linux-x64 -t:PublishContainer` |
| Pack the Aspire package | `dotnet pack src/Stabilt.TellusTalk.Emulator.Aspire -c Release` |

## Traps

- Debug builds hide analyzer errors. Build `-c Release` before pushing.
- Time comes from `TimeProvider`; `DateTime.Now` and its kin are banned in `BannedSymbols.txt`.
- The report service's timer may start after a test has moved the fake clock. A test waiting for a report moves the
  clock a second at a time.
- The read API's JSON is a contract other repositories test against. `ReadApiTests` pins every value; change it
  deliberately and update the README with it.
- The page must work without its script, as plain linked pages. The script swaps parts by their ids, so a part it swaps
  keeps its `id`.
- Analyzers can misread C# 14 extension blocks; suppress what they report there with a reason.
- Messages live in memory; a restart empties the lists.
