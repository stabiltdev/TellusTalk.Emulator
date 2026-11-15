---
status: accepted
date: 2026-11-15
deciders: [Peter Hedberg]
---

# ADR 20261115-ship-a-package-that-reads-what-the-emulator-caught: Ship a package that reads what the emulator caught, through the emulator's own API only

## Context and Problem Statement

A test in another repository runs code that sends SMS to the emulator, then checks what was sent: the text, the
sender, `echo`, the report address. Without a package it writes its own models of the read API's JSON, or picks through
`JsonElement`, and builds its own URLs, where a `+` left unescaped in `?to=` arrives as a space and finds nothing.
Version 1.0.0 fixes the read API's shape, so a reader outside the server is worth having before then.

## Considered Options

- Keep the README's `HttpClient` lines for each test to copy.
- Read the messages from the Testcontainers package.
- Publish a package of its own that reads the emulator's API.

## Decision Outcome

**`Stabilt.TellusTalk.Emulator.Client` lists, reads and clears what the emulator caught through the emulator's own
API, and calls nothing of TellusTalk's; one version tag publishes it with the image and the other packages, at the
same version.** `TellusTalkEmulatorClient` takes an `HttpClient` whose base address is the emulator's, or the address
itself. `GetMessagesAsync` lists with a `MessageQuery` of `State`, `Channel`, `To` and `Search`, each escaped;
`GetMessageAsync` reads one message, `null` when there is none; `DeleteMessagesAsync` clears the lists. A message is a
`CaughtMessage` with every property the read API serves, the request's fields as JSON values.

A package of its own serves a test whatever started the emulator, where the Testcontainers package would leave out an
Aspire app or a `docker compose` file. Its models are its own rather than the server's response records: those are
built from internal types, so sharing them would make each internal rename a breaking change of the package. Instead
the package's tests run against the emulator's host in memory, and a model that drifts from the server fails here. It
targets `net10.0`, the oldest .NET in support, and depends on nothing beyond the framework.

It lists, reads and clears, and nothing more: the tests it serves read once the code under test has returned, so they
need no wait, and the event stream stays the page's. Arming send failures, reports on request and a wait for a message
can each be added when a test needs it, without breaking what is there.

## Consequences

- **Good:** a test asserts on typed properties and filters without escaping by hand, and a change to the read API that
  the package does not follow fails this repository's tests rather than a consumer's.
- **Bad:** the read API has a second reader in the repository that changes with it; a value an older package does not
  know, such as a new channel from a newer image, fails the read, so the package's version has to follow the image's;
  and the first release needs nuget.org's Trusted Publishing policy to allow the new package id.
- **Follow-up:** none.
