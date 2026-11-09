---
status: accepted
date: 2026-11-09
deciders: [Peter Hedberg]
supersedes: [ADR 20261109-fail-a-counted-number-of-sends-on-request](20261109-fail-a-counted-number-of-sends-on-request.md)
---

# ADR 20261109-fail-a-counted-number-of-sends-late-if-asked: Fail a counted number of sends with a 4xx or 5xx, late if a test asks

## Context and Problem Statement

[ADR 20261109-fail-a-counted-number-of-sends-on-request](20261109-fail-a-counted-number-of-sends-on-request.md) let a
test arm only a 5xx, answered at once. A client's timeout could not be tried, nor its handling of the permanent errors
the specification gives as `4XX`. K8 has been seen answering only one of those, the `401` for missing credentials.

## Considered Options

- Keep armed failures to a 5xx answered at once.
- Let an armed failure carry any 4xx or 5xx and a delay before it is answered.
- Arm delays on an endpoint of their own, apart from the failures.

## Decision Outcome

**A test arms failures with `PUT /api/v1/send-failures`: a status from 400 to 599, a count and a delay in seconds. Each
of the next sends with credentials waits the delay, is answered with the status and TellusTalk's headers, and records
nothing. A `401` takes the form TellusTalk was seen giving; any other status the specification's, its reason phrase as
`text/plain`.** `SendFailures` holds them; `GET` reads what is left and `DELETE` clears it.

One armed failure says what the client meets: how long it waits, then what it gets. A timeout is a delay longer than the
client's own, so the emulator need not know the client's setting. Delays armed apart from failures would spend
separately, and a test would have to reason about which send met which. A count still lets a test say "fail twice,
then succeed" and read how many failures its client spent, which neither a number that always fails nor a probability
allows.

## Consequences

- **Good:** a client's retry, give-up and timeout paths can be tried and asserted on.
- **Bad:** a late send holds its request open for the delay; the failures belong to the whole emulator, so tests sharing
  one instance in parallel can spend each other's; a late success cannot be armed; and every form but the `401` rests
  on the specification.
- **Follow-up:** none.
