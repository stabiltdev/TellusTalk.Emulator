---
status: accepted
date: 2026-11-20
deciders: [Peter Hedberg]
---

# ADR 20261120-answer-a-send-as-k8-does: Answer a send as K8 does: invalid addresses listed, invalid recipients blocked with a reason

## Context and Problem Statement

Probes on 2026-11-17 ([k8-behaviour.md](../k8-behaviour.md)) showed K8 taking requests the emulator took too, but
answering them differently. An answer to several recipients had no `log_uuid`. A `report_to` or `reply_to` that is no
address was listed in `invalid_report_to` or `invalid_reply_to`. An `sms:` number with a letter was listed in
`invalid_recipients`, counted in `job_size` and blocked as `txtNUMBER_NOT_NUMERIC`, never sent. The emulator answered
every send alike and sent such a number with a warning, so a client reading the answer, or a test counting what went
out, would see something K8 never shows.

## Considered Options

- Keep accepting these requests with a warning, and answer every send alike.
- List what K8 lists in the answer, and catch a number K8 blocks as a message blocked with its reason.
- Refuse a request holding an address K8 cannot use.

## Decision Outcome

**The answer lists what K8 lists, in K8's places, and leaves `log_uuid` out for several recipients; an `sms:` number K8
blocks is caught as a message blocked for that reason.** `SendResponse` writes `job_id`, `job_uuid`,
`invalid_report_to`, `invalid_reply_to`, `log_uuid`, `invalid_recipients` and `job_size`, each list and `log_uuid` only
when it has something. A message's `BlockedReason` is `Duplicate` for a held-back send or `NumberNotNumeric` for a
number that, without its spaces and hyphens, holds anything but digits after an optional `+`; any reason makes it
`blocked`, keeps it from falling due and from being reported, and makes it no original for the duplicate rule.

K8 answered these requests `200`, so refusing them would be stricter than K8, which
[ADR 20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest](20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest.md)
rules out. Accepting with a warning alone hides what K8 tells the client and lets a test count a message as sent that
K8 never sends. A blocked number is kept as a message rather than dropped, as a held-back duplicate is in
[ADR 20261114-hold-back-a-duplicate-as-k8-does](20261114-hold-back-a-duplicate-as-k8-does.md), because K8's admin log
shows it and a test can assert on it; the reason is a value of the emulator's own, written in camelCase like `state`,
while the page shows K8's `txtNUMBER_NOT_NUMERIC`.

## Consequences

- **Good:** a client meets K8's answers locally, lists included, and a test sees a blocked number as blocked, with why.
- **Bad:** an answer to several recipients has no `log_uuid`, and a number with a letter is no longer sent; both break
  clients and tests that relied on them. The read API and the client package gain `blockedReason`, a contract change
  for every repository that reads it.
- **Follow-up:** a blocked number gets no delivery report until issue #31 settles what K8 reports for one. Where the
  lists sit with several recipients, and what K8 lists besides an address that is none at all, wait for a probe round.
