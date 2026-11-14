---
status: accepted
date: 2026-11-14
deciders: [Peter Hedberg]
---

# ADR 20261114-fail-reports-only-for-the-numbers-a-setting-names: Fail delivery reports only for the numbers a setting names

## Context and Problem Statement

`Emulator__FailingNumbers` names the numbers whose delivery reports say the operator could not deliver. It defaulted to
`+46701740699`, the last number of PTS's range for fiction and an easy pick for a test, which then met failure reports
it never asked for; TellusTalk has no counterpart to a number that always fails. A failure report is already available
on demand, through `POST /api/v1/messages/{id}/reports` or the page's button. In both packages, a second
`WithFailingNumbers` call replaced the first.

## Considered Options

- Keep the default and document it better.
- No default, with the setting kept as one comma-separated string.
- No default, with the setting made a list of indexed keys, `Emulator__FailingNumbers__0`, `__1`.

## Decision Outcome

**Delivery reports fail only for the numbers `Emulator__FailingNumbers` names, none by default; the setting stays one
comma-separated string, and each `WithFailingNumbers` call in the Aspire and Testcontainers packages adds its numbers
to those of earlier calls.** `EmulatorOptions.FailingNumbers` is empty unless set.

The surprise was in the default, not in how it was described. A list of plain values stays one string because that is
easy to give with `docker run`, while indexed keys bring the configuration binder's pitfalls: items bound to a list are
appended to its default, and a list from a source of higher priority merges with a lower one by index instead of
replacing it, so it cannot be shortened.

## Consequences

- **Good:** no test meets a failure it did not ask for, and a list of failing numbers can be built in steps.
- **Bad:** an emulator nobody configured has no failing number, which breaks a test that relied on `+46701740699`; a
  second `WithFailingNumbers` call no longer replaces the first, and no call removes a number once added.
- **Follow-up:** a list of objects, such as report recipients in settings, takes indexed keys, since no single string
  describes it well.
