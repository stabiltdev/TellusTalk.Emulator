---
status: accepted
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-read-a-time-without-an-offset-as-swedish-time: Read a scheduled time without an offset as Swedish time

## Context and Problem Statement

`scheduled_send_time` is an ISO 8601 date and time, and nothing in the specification says how TellusTalk reads one
without an offset; it was not probed. The emulator must still decide when such a message falls due. The .NET container
images without the `-extra` suffix carry no time zone data, so a named zone cannot be looked up in them.

## Considered Options

- Read it as UTC.
- Read it as Swedish local time, `Europe/Stockholm`.

## Decision Outcome

**A `scheduled_send_time` without an offset is read as Swedish local time, `Europe/Stockholm`, and the image is built on
the `noble-chiseled-extra` family, which carries time zone data.** `ScheduledTime` does the reading; the csproj's
`ContainerFamily` names the image. The README lists it as unobserved.

TellusTalk is a Swedish service whose senders mostly reach Swedish numbers, so a time written without an offset most
likely means Swedish time. It is the emulator's reading until someone observes TellusTalk's.

## Consequences

- **Good:** a message scheduled in Swedish local time falls due when its sender expects.
- **Bad:** the `-extra` image is larger than the plain chiseled one; if TellusTalk turns out to read UTC, such a message
  falls due an hour or two off.
- **Follow-up:** observe how TellusTalk reads a time without an offset.
