---
status: accepted
date: 2026-11-24
deciders: [Peter Hedberg]
supersedes: [ADR 20261110-read-a-time-without-an-offset-as-swedish-time-on-an-extra-image](20261110-read-a-time-without-an-offset-as-swedish-time-on-an-extra-image.md)
---

# ADR 20261124-read-a-time-without-an-offset-as-utc: Read a scheduled time without an offset as UTC, as K8 does

## Context and Problem Statement

[ADR 20261110-read-a-time-without-an-offset-as-swedish-time-on-an-extra-image](20261110-read-a-time-without-an-offset-as-swedish-time-on-an-extra-image.md)
read a `scheduled_send_time` without an offset as Swedish time, a reading to hold until someone observed K8's. On
2026-11-24, three sends were scheduled for the same wall time: one without an offset, one with `Z` and one with
`+01:00`. The one without an offset went out with the `Z` one, an hour after the `+01:00` one, so K8 reads such a time
as UTC.

## Considered Options

- Read it as UTC, as K8 does.
- Keep reading it as Swedish time.

## Decision Outcome

**A `scheduled_send_time` without an offset is read as UTC.** `ScheduledTime` reads it with
`DateTimeStyles.AssumeUniversal`.

The emulator follows what K8 was seen to do
([ADR 20261114-record-each-observation-of-k8-with-its-date](20261114-record-each-observation-of-k8-with-its-date.md)):
a client tested against it has to see a scheduled message fall due when K8 would send it.

## Consequences

- **Good:** a message scheduled without an offset falls due when it would in K8, and the reading needs no time zone
  data, nor a case for the hour skipped in spring or for times before Stockholm had a standard offset.
- **Bad:** a client that writes Swedish local time without an offset sees its message an hour or two late, in the
  emulator as in K8, and nothing warns it.
- **Follow-up:** the image stays on the `-extra` family, whose time zone data the reading no longer needs. The family
  also carries ICU, which the emulator runs with, so moving to the plain chiseled family means running with invariant
  globalization, a choice of its own.
