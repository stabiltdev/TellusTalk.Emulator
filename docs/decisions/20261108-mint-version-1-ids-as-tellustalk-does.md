---
status: accepted
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-mint-version-1-ids-as-tellustalk-does: Mint version 1 UUIDs as TellusTalk does

## Context and Problem Statement

TellusTalk's ids (`job_id`, `job_uuid`, `log_uuid`, `X-Request-Id` and the request id on a delivery report) were
observed to be version 1 UUIDs, written as 32 lowercase hexadecimal characters without hyphens. .NET mints version 4
(`Guid.NewGuid`) and version 7 (`Guid.CreateVersion7`), but not version 1.

## Considered Options

- `Guid.NewGuid().ToString("N")`: the same shape with a version 4 underneath.
- Mint version 1 in the emulator.

## Decision Outcome

**Every id the emulator hands out is a version 1 UUID written as 32 lowercase hexadecimal characters:
`Guid.CreateVersion1(timestamp).ToString("N")`.** `Guid.CreateVersion1` is a C# 14 static extension in
`Ids/GuidExtensions.cs`, with the overloads of `Guid.CreateVersion7`. The time comes from the injected `TimeProvider`,
and the clock sequence and the node are drawn at random for each id, so ids minted in the same tick stay apart without
the state a counter would need.

A client would hardly notice a version 4 in the same shape. Matching TellusTalk exactly costs one static method in the
style .NET already uses for version 7, so the emulator matches.

## Consequences

- **Good:** the ids look exactly like TellusTalk's and carry the time they were minted.
- **Bad:** ids of the same tick are kept apart by 62 random bits rather than by a counter; the overload without a time
  reads the system clock, an exception to the rule that time comes from `TimeProvider`.
- **Follow-up:** analyzers may misread C# 14 extension blocks; suppress what they report there, with a reason, until
  their support catches up.
