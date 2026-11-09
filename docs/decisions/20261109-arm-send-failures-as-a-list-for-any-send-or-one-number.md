---
status: accepted
date: 2026-11-09
deciders: [Peter Hedberg]
supersedes: [ADR 20261109-fail-a-counted-number-of-sends-on-request](20261109-fail-a-counted-number-of-sends-on-request.md)
---

# ADR 20261109-arm-send-failures-as-a-list-for-any-send-or-one-number: Arm send failures as a list, for any send or for one number

## Context and Problem Statement

[ADR 20261109-fail-a-counted-number-of-sends-on-request](20261109-fail-a-counted-number-of-sends-on-request.md) let a
test arm one 5xx for the next sends, answered at once, for any number. Much of what a client does with failures could
not be tried: its timeout, its handling of the permanent errors the specification gives as `4XX`, and a retry that
meets different answers in turn, which a test cannot re-arm between since the client retries inside one call. And the
failures belonged to the whole emulator, so tests sharing one instance in parallel spent each other's. K8 has been seen
answering only one failure, the `401` for missing credentials.

## Considered Options

- Keep one 5xx, answered at once, for any send.
- Arm a list of failures, each with a 4xx or 5xx status, a count and a delay, for any send or for one recipient number.
- Arm delays apart from statuses, on endpoints of their own.
- Leave the failures global, and have tests that need them apart run an emulator each.

## Decision Outcome

**A test arms a list of failures with `PUT /api/v1/send-failures` for any send, or `PUT /api/v1/send-failures/{number}`
for sends to one number. Each has a status from 400 to 599, a count and a delay in seconds; the next sends spend them in
order, each waiting its delay, then answered with the status and TellusTalk's headers, and recording nothing. Failures
for any send are spent before the request is read; a number's once it is read, by the first recipient in `to` that has
any, one per send. A `401` takes the form TellusTalk was seen giving, every other status the specification's.**
`SendFailures` holds them; `GET` reads what is left, and `DELETE` clears a number or everything.

A list lets a client's own retries meet different answers within one call. A delay belongs to the failure it precedes,
so the order of what a client meets cannot drift between two lists, and a timeout is a delay longer than the client's
own, which the emulator need not know. A test already chooses its recipient, so a number keeps parallel tests apart
without a container each. Failures for any send stand for a TellusTalk that is down, which answers whatever the request
holds. A count per entry still lets a test say "fail twice, then succeed", which a number that always fails, as
delivery reports have, cannot.

## Consequences

- **Good:** a client's retry, timeout and give-up paths can be tried and asserted on, and tests keep their failures
  apart by number.
- **Bad:** the read API's send-failure answers are lists, unlike the single object released in 0.3.0; a late send holds
  its request open for the delay; a send to several armed numbers spends one failure; a late success cannot be armed;
  and every form but the `401` rests on the specification.
- **Follow-up:** none.
