---
status: superseded by [ADR 20261111-ship-an-aspire-package-on-the-oldest-aspire-without-known-vulnerabilities](20261111-ship-an-aspire-package-on-the-oldest-aspire-without-known-vulnerabilities.md)
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-ship-an-aspire-package-of-the-images-version: Ship an Aspire package that runs the image of its own version

## Context and Problem Statement

Apps that run the emulator from an Aspire AppHost each need the same lines: the image and its tag, the container's
port, a health check, and the emulator's address handed to the app under the setting its TellusTalk client reads. Two
apps were about to add them. Copied lines drift from the image they name, and the README's example says nothing about
settings such as the failing numbers.

## Considered Options

- Keep the AppHost lines in the README for each app to copy.
- Publish an Aspire package that adds the emulator.

## Decision Outcome

**`Stabilt.TellusTalk.Emulator.Aspire` adds the emulator to an AppHost and runs the image of its own version; one version
tag publishes the image and then the package.** `AddTellusTalkEmulator` sets the image, the port and the health check;
`WithAccountSender`, `WithFailingNumbers`, `WithReportDelay` and `WithRetentionLimit` set the emulator's settings; and
`WithReference` gives an app the address as `TellusTalk__BaseUrl`, the setting `TellusTalk:BaseUrl` a TellusTalk client
reads, or under a name the app chooses. The methods live in Aspire's own namespaces, as its integrations' do.

The package targets `net8.0` and asks for `Aspire.Hosting` 9.5.2, so AppHosts on any supported .NET and on Aspire 9.5
or later can use it without being made to upgrade; 9.0 brought a `KubernetesClient` with a known vulnerability. It pins
`MessagePack`, which `Aspire.Hosting` 9.5.2 brings through `StreamJsonRpc`, past its known vulnerabilities.

## Consequences

- **Good:** an AppHost adds the emulator in one line and gets settings by name; the package and the image it runs never
  drift apart.
- **Bad:** the package follows Aspire's hosting API, which changes often, and publishing it needs a nuget.org key in
  the repository's secrets.
- **Follow-up:** none.
