# Conventions

How code and tests are written here. A convention is a summary that cites its source; if this page and the code
disagree, the code is right and the page is a bug.

## Endpoints

**Source:** code: `src/Stabilt.TellusTalk.Emulator/Http/`, `tests/Stabilt.TellusTalk.Emulator.Tests/Http/EndpointConventionTests.cs`

- An endpoint is an `internal sealed class` named with an `Endpoint` suffix that implements `IEndpoint`. Its `Map` only
  wires the route to a `private static Handle`; `MapEndpoints` finds every one at startup, so `Program.cs` lists none.
  `EndpointConventionTests` fails on an endpoint that is not sealed or lacks the suffix.
- One file per endpoint, in the folder of the resource it serves: `Messages/`, `Reports/`, `Sending/`, `Page/`. What only
  one endpoint uses, such as a small request or response record, stays in its file.
- The read API lives under `api/v1/`. TellusTalk's own paths, `send/v1` alone for now, keep TellusTalk's names, status
  codes and headers.
- `send/v1` requires authentication; `BasicAuthenticationHandler` accepts any credentials and refuses a request without
  them with TellusTalk's own `401`. Nothing else requires it.

## The read API's answers

**Source:** [ADR 20261108-follow-observed-behaviour-before-the-specification](decisions/20261108-follow-observed-behaviour-before-the-specification.md);
code: `src/Stabilt.TellusTalk.Emulator/Messages/MessageResponse.cs`, `tests/Stabilt.TellusTalk.Emulator.Tests/Api/ReadApiTests.cs`

- An answer is a `…Response` record built by a `static From(...)` from the domain object; its parts are records of their
  own in the same file, each with its own `From`.
- An enum goes out as itself and is written as its name in camelCase by the one `JsonStringEnumConverter` in
  `Program.cs`: `MessageState.Scheduled` is `"scheduled"`.
- The read API's JSON is a contract that tests in other repositories depend on, and `ReadApiTests` pins every value. A
  change to it is deliberate, and the README's description changes with it.
- A `400` from the read API carries `ApiError`: `{"message": "..."}`.

## Reading a send request

**Source:** [ADR 20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest](decisions/20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest.md),
[ADR 20261108-know-every-field-of-every-send-schema](decisions/20261108-know-every-field-of-every-send-schema.md);
code: `src/Stabilt.TellusTalk.Emulator/Requests/`

- Code reads a request's fields through `SendRequest.Body`, whose properties are `JsonElement`s: `Body.SmsFlash`, never
  a lookup by name. A field the request left out is `JsonValueKind.Undefined`, one sent as null is `Null`.
- A new field goes on `SendRequestBody` with its `[JsonPropertyName]`, and with `[Expects]` when the schemas fix its
  shape.
- `SendRequest.Fields` holds the fields as they arrived, unknown ones included, for what is shown as sent: the page's
  other fields and the read API's `fields`.
- What the specification forbids becomes a `Warning` with a code from `WarningCodes`, never a refusal. A refusal of the
  emulator's own begins `TellusTalk Emulator:` and says it is the emulator's limit.

## Time and ids

**Source:** [ADR 20261108-mint-version-1-ids-as-tellustalk-does](decisions/20261108-mint-version-1-ids-as-tellustalk-does.md);
code: `BannedSymbols.txt`, `src/Stabilt.TellusTalk.Emulator/Ids/GuidExtensions.cs`

- Time comes from the injected `TimeProvider`. `DateTime.Now`, `DateTime.UtcNow` and `DateTimeOffset.UtcNow` are banned
  and fail the Release build.
- An id is `Guid.CreateVersion1(time.GetUtcNow()).ToString("N")`: a version 1 UUID in TellusTalk's form.

## Tests

**Source:** code: `tests/Stabilt.TellusTalk.Emulator.Tests/Support/`

- xUnit v3 on Microsoft Testing Platform, and FluentAssertions 8, used under its licence for non-commercial and
  open-source work. Run them with `dotnet test --solution TellusTalk.Emulator.slnx`, narrowed with `--filter-class`.
- A test of behaviour over HTTP starts the whole host with `EmulatorFactory`, whose `FakeTimeProvider` starts at
  2026-11-07 22:00 UTC and whose `Reports` catches every delivery report instead of the network. `Settings` takes host
  settings before the first request.
- A test name is the rule it pins, as a sentence: `A_number_set_to_fail_gets_a_failure_report`.
- The report service's timer may start after a test has moved the fake clock, so a test waiting for a report moves the
  clock a second at a time rather than in one jump.
- Before code that shapes an answer is rewritten, a test pins what the answer is; the rewrite then keeps it green.
- Numbers come from PTS's range for fiction, `+46701740605` to `+46701740699`; names and texts are invented and name no
  real person, company or product.

## Code style

**Source:** code: `.editorconfig`, `Directory.Build.props`

- LINQ is a method chain, never query syntax, with one call per line once there are two; separate conditions are
  separate `Where` calls. An assertion chain stays on one line.
- A comment says what the code cannot: a constraint, a rejected alternative, a consequence out of sight. It never
  restates a name, and it describes the code as it is, not the change that made it.
- An argument guard such as `ArgumentNullException.ThrowIfNull` belongs on a public method of a public type only.
- An interface needs a purpose: something that uses the abstraction. A class without state is static and unregistered.
- Packages are versioned centrally in `Directory.Packages.props`.
- Analyzers run in every build and fail the Release build: build `-c Release` before pushing, and keep
  `dotnet format --verify-no-changes` clean. A suppression names its reason.
