---
status: accepted
date: 2026-11-11
deciders: [Peter Hedberg]
supersedes: [ADR 20261108-ship-an-aspire-package-of-the-images-version](20261108-ship-an-aspire-package-of-the-images-version.md)
---

# ADR 20261111-ship-an-aspire-package-on-the-oldest-aspire-without-known-vulnerabilities: Ship an Aspire package that runs the image of its own version, on the oldest Aspire without a known vulnerability

## Context and Problem Statement

[ADR 20261108-ship-an-aspire-package-of-the-images-version](20261108-ship-an-aspire-package-of-the-images-version.md)
had the package ask for `Aspire.Hosting` 9.5.2, so AppHosts on Aspire 9.5 or later could take it. GHSA-f7vh-h5g3-7jrh
then reported an elevation of privilege in every `Aspire.Hosting` before 13.6.5, and the package audit at restore fails
the build on it.

## Considered Options

- Ask for `Aspire.Hosting` 13.6.5, the first version without the vulnerability.
- Keep 9.5.2 and suppress the advisory, leaving each AppHost's version to the AppHost's own audit.

## Decision Outcome

**`Stabilt.TellusTalk.Emulator.Aspire` adds the emulator to an AppHost and runs the image of its own version; one version
tag publishes the image and then the package. It targets `net8.0` and asks for the oldest `Aspire.Hosting` without a
known vulnerability.** `AddTellusTalkEmulator` sets the image, the port and the health check; `WithAccountSender`,
`WithFailingNumbers`, `WithReportDelay` and `WithRetentionLimit` set the emulator's settings; and `WithReference` gives
an app the address as `TellusTalk__BaseUrl`, the setting `TellusTalk:BaseUrl` a TellusTalk client reads, or under a name
the app chooses. The methods live in Aspire's own namespaces, as its integrations' do. The csproj names the version,
13.6.5 for now.

A package that asks for a vulnerable version leaves every AppHost that has not moved past it exposed, and a suppressed
advisory would hide the next one as well. The oldest safe version still lets an AppHost move no further than it must.
The record names the rule rather than the version, so the next advisory changes the csproj and not this record.

## Consequences

- **Good:** an AppHost adds the emulator in one line and gets settings by name; the package and the image it runs never
  drift apart; and an AppHost that takes the package is past every known vulnerability in Aspire's hosting.
- **Bad:** an AppHost on an older Aspire has to upgrade to take the package; the package follows Aspire's hosting API,
  which changes often; and publishing it needs nuget.org's Trusted Publishing policy for the repository.
- **Follow-up:** none.
