---
status: accepted
date: 2026-11-10
deciders: [Peter Hedberg]
supersedes: [ADR 20261108-read-a-time-without-an-offset-as-swedish-time](20261108-read-a-time-without-an-offset-as-swedish-time.md)
---

# ADR 20261110-read-a-time-without-an-offset-as-swedish-time-on-an-extra-image: Read a scheduled time without an offset as Swedish time, on an image with time zone data

## Context and Problem Statement

[ADR 20261108-read-a-time-without-an-offset-as-swedish-time](20261108-read-a-time-without-an-offset-as-swedish-time.md)
named the image family `noble-chiseled-extra`, on Ubuntu 24.04. The images for .NET 11 are built on Ubuntu 26.04,
`resolute`, and none is published on `noble`, so the record named a family the emulator can no longer use. How
TellusTalk reads a `scheduled_send_time` without an offset is still unobserved, and the .NET container images without
the `-extra` suffix still carry no time zone data.

## Considered Options

- Read it as UTC.
- Read it as Swedish local time, `Europe/Stockholm`, on an image family with time zone data.

## Decision Outcome

**A `scheduled_send_time` without an offset is read as Swedish local time, `Europe/Stockholm`, and the image is built on
a chiseled `-extra` family, which carries time zone data, on the Ubuntu release the targeted .NET is published for.**
`ScheduledTime` does the reading; the csproj's `ContainerFamily` names the family, `resolute-chiseled-extra` for .NET
11. The README lists the reading as unobserved.

TellusTalk is a Swedish service whose senders mostly reach Swedish numbers, so a time written without an offset most
likely means Swedish time. It is the emulator's reading until someone observes TellusTalk's. The record names the kind
of family rather than its Ubuntu release, so moving to the next .NET changes the csproj and not this record.

## Consequences

- **Good:** a message scheduled in Swedish local time falls due when its sender expects.
- **Bad:** the `-extra` image is larger than the plain chiseled one; if TellusTalk turns out to read UTC, such a message
  falls due an hour or two off.
- **Follow-up:** observe how TellusTalk reads a time without an offset.
