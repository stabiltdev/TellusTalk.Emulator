---
status: superseded by [ADR 20261109-arm-send-failures-as-a-list-for-any-send-or-one-number](20261109-arm-send-failures-as-a-list-for-any-send-or-one-number.md)
date: 2026-11-09
deciders: [Peter Hedberg]
---

# ADR 20261109-fail-a-counted-number-of-sends-on-request: Fail a counted number of sends when a test asks for it

## Context and Problem Statement

The specification says a `5XX` from the send API is a temporary error and asks a client to retry. A client's retry path
could not be tried against the emulator, which answers every authenticated send. K8 has not been seen failing, so
neither the moment nor the form of a real failure is known.

## Considered Options

- A recipient number whose sends always fail, as `Emulator__FailingNumbers` does for delivery reports.
- A probability of failure, as Mailpit's Chaos does for SMTP.
- A count of failures with a status, armed through the read API.

## Decision Outcome

**A test arms failures with `PUT /api/v1/send-failures`, a 5xx status and a count; each of the next sends with
credentials is answered with that status, its reason phrase as `text/plain` and TellusTalk's headers, and records
nothing.** `SendFailures` holds them; `GET` reads what is left and `DELETE` clears it.

A number that always fails cannot try a retry that succeeds. A probability makes a test flaky. A count lets a test say
"fail twice, then succeed" and read afterwards how many failures its client spent. The form follows the specification,
since no real failure has been seen, and the README lists it.

## Consequences

- **Good:** a client's retry and give-up paths can be tried and asserted on.
- **Bad:** the failures belong to the whole emulator, so tests sharing one instance in parallel can spend each other's;
  and the answer's form rests on the specification.
- **Follow-up:** none.
