# Contributing

Issues and pull requests are welcome. The emulator is a small tool kept by one person, so a short note on what you are
after goes a long way.

## Issues

- **A bug:** what you sent, what the emulator answered, and what you expected. Leave out credentials and anything that
  identifies a person.
- **Something TellusTalk does differently:** the most useful report of all. The emulator follows what TellusTalk was
  seen to do, and the README lists what it has not been seen to do yet. Say what you sent to TellusTalk and what came
  back, with the same care for credentials and personal data.
- **A larger change:** open an issue before the pull request, so the approach can be agreed before the work is done.

## Pull requests

- One coherent change per pull request, with tests for the behaviour it adds or changes.
- The title follows [Conventional Commits](https://www.conventionalcommits.org/): `fix: answer a send without a sender
  with the account's sender`.
- CI builds, checks formatting and runs the tests. A first pull request waits for a maintainer to approve its run.
- Contributions are made under the [MIT licence](LICENSE).

## Building and testing

```shell
dotnet build Stabilt.TellusTalk.Emulator.slnx -c Release
dotnet format Stabilt.TellusTalk.Emulator.slnx --verify-no-changes
dotnet test --solution Stabilt.TellusTalk.Emulator.slnx
```

Build in Release before pushing: analyzer warnings fail it, and a Debug build hides them. The tests need neither Docker
nor a TellusTalk account.

## How the code is written

- [docs/conventions.md](docs/conventions.md) says how code and tests are written here.
- [docs/decisions](docs/decisions/README.md) says why the emulator works as it does. Read the record before changing
  something that looks odd; most odd things are on purpose. A change to a decision comes with a new record.
- Test data uses numbers from PTS's range for fiction, `+46701740605` to `+46701740699`, and invented names and texts.
