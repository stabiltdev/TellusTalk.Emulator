---
status: accepted
date: 2026-11-20
deciders: [Peter Hedberg]
---

# ADR 20261120-refuse-a-send-as-k8-was-seen-to: Refuse a send as K8 was seen to, its server errors included

## Context and Problem Statement

Probes on 2026-11-17 ([k8-behaviour.md](../k8-behaviour.md)) showed K8 refusing, with texts of its own, a dozen kinds
of request that the emulator accepted with at most a warning or refused as its own limit: `to` or `text` missing, empty
or of another type; `report_to` and `reply_to`, the counts and `sms_header` of the wrong type; a character set that is
no string or none of the four; a binary SMS without `sms_udh`; a `billing_id` with a space and a hyphen. Two forms of
`attachments` K8 answered with a `500`. A client that works against the emulator could fail against K8.

## Considered Options

- Keep accepting these requests with a warning.
- Refuse them with K8's own status and text, in the order K8 checks.
- Refuse them as the emulator's own limit, with a `TellusTalk Emulator:` text.

## Decision Outcome

**What K8 was seen to refuse is answered `400`, and what it was seen to fail on `500`, each with K8's text as
`text/html; charset=utf-8`; of several rules a request breaks, the first in K8's order decides.** `SendRequestRules`
holds them as an ordered list whose first hit wins: `to`, `text`, `attachments`, `reply_to`, `report_to`,
`sms_validity_time`, `sms_max_parts`, the character set, `sms_header`, the sender fields and `billing_id`, all before the
emulator's own limits on `to`. A refusal is a `RefusedByTellusTalk`, a server error a `FailedInTellusTalk` answered as a
`TellusTalkError`. Nothing is recorded either way.

K8's texts name Python's checks, and the rules follow those checks rather than only the values sent: "missing or empty"
is read as Python's truth test, so `0`, `false`, `[]` and `{}` are missing too, and `attachments` fails wherever
iterating it and calling `pop` on each item would. So "what TellusTalk was seen to refuse", the rule of
[ADR 20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest](20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest.md),
is read as the check K8's text names, as the sender's types were in
[ADR 20261114-choose-and-refuse-the-sms-sender-as-k8-does](20261114-choose-and-refuse-the-sms-sender-as-k8-does.md).
Where two rules were not seen against each other, the order is the emulator's and keeps every order that was seen.
Each such reading is listed under "Not observed" in k8-behaviour.md.

Warning lets a request pass that K8 refuses, so a client's mistake surfaces only in production, as
[ADR 20261114-choose-and-refuse-the-sms-sender-as-k8-does](20261114-choose-and-refuse-the-sms-sender-as-k8-does.md)
found for the sender. Refusing as the emulator's limit would tell the client the emulator is at fault, where K8 refuses
the same request with words of its own.

## Consequences

- **Good:** a client meets K8's refusals and server errors locally, word for word.
- **Bad:** requests the emulator accepted are refused, and the warnings `character-set-invalid`, `binary-without-udh`
  and `billing-id-invalid` are gone. Where a rule rests on reading K8's text rather than on a value sent, the emulator
  may refuse what K8 accepts, or accept what it refuses.
- **Follow-up:** the readings listed as not observed wait for a probe round.
