---
status: accepted
date: 2026-11-11
deciders: [Peter Hedberg]
---

# ADR 20261111-ship-a-testcontainers-package-of-the-images-version: Ship a Testcontainers package that runs the image of its own version

## Context and Problem Statement

A test project that runs the emulator from Testcontainers copies the README's lines: the image and its tag, the port, a
wait for the health endpoint, and the base address made from the mapped port, with each setting an environment variable
whose name has to be spelt right. Copied lines drift from the image they name, as they did for Aspire AppHosts before
[ADR 20261108-ship-an-aspire-package-of-the-images-version](20261108-ship-an-aspire-package-of-the-images-version.md).

## Considered Options

- Keep the README's `ContainerBuilder` lines for each test project to copy.
- Publish a Testcontainers module that builds the emulator's container.

## Decision Outcome

**`Stabilt.TellusTalk.Emulator.Testcontainers` builds the emulator's container and runs the image of its own version;
one version tag publishes the image, then this package beside the Aspire package.** `TellusTalkEmulatorBuilder` sets
the image, the port and a wait for the health endpoint; `WithAccountSender`, `WithFailingNumbers`, `WithReportDelay` and
`WithRetentionLimit` set the emulator's settings as the Aspire package's methods do; and
`TellusTalkEmulatorContainer.GetBaseAddress` gives the address an app takes in TellusTalk's place.

The types take the shape of Testcontainers' own modules, a builder and a container with `GetBaseAddress`, but live in
the package's own namespace: a test file needs a `using` for any module, so borrowing Testcontainers' would gain
nothing. Testcontainers' modules take their image in the constructor and are dropping their defaults, which go stale;
this package's default is the image released with it, so `new TellusTalkEmulatorBuilder()` runs that, and
`new TellusTalkEmulatorBuilder(image)` another.

The package targets `net8.0` and asks for `Testcontainers` 4.16.0, the version its tests run with; Testcontainers is a
test project's dependency, so moving one up to it costs little. The tests start the emulator in Docker from an image
built from the same commit, named by `TELLUSTALK_EMULATOR_TEST_IMAGE`. CI builds that image before the tests; without
it they are skipped, so the other tests still need no Docker.

## Consequences

- **Good:** a test project adds the emulator in a few lines and gets settings by name; the package and the image it
  runs never drift apart, and CI tries one against the other.
- **Bad:** a new setting goes into both packages; every CI run builds the image; and the first release of the package
  needs nuget.org's Trusted Publishing policy to allow publishing a new package.
- **Follow-up:** none.
