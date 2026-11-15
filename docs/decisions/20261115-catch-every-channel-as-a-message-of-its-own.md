---
status: accepted
date: 2026-11-15
deciders: [Peter Hedberg]
supersedes: [ADR 20261109-catch-the-sms-recipients-and-name-the-others](20261109-catch-the-sms-recipients-and-name-the-others.md)
---

# ADR 20261115-catch-every-channel-as-a-message-of-its-own: Catch a recipient on every channel as a message of its own

## Context and Problem Statement

Version 1.0.0 fixes the read API's shape, so what it says about other channels has to be settled before it is fixed.
[ADR 20261109-catch-the-sms-recipients-and-name-the-others](20261109-catch-the-sms-recipients-and-name-the-others.md)
caught a request's SMS recipients and only named the rest, and the emulator refused a request K8 accepts when none of
its recipients was on `sms:`. A client sending an e-mail, a fax or a voice call could not run against it.

## Considered Options

- Keep naming the other channels on the SMS messages.
- One message type with a channel, and an SMS part that may be absent.
- A type per channel.
- A list of their own beside the SMS messages.

## Decision Outcome

**Every `sms:`, `email:`, `fax:`, `mms:`, `voice:` and `group:` recipient is caught as a message of its own, with a
`channel` in the read API; `sender`, `links` and `parts` are `null` for a channel other than SMS.** A caught message
carries its channel and an SMS part that is absent elsewhere, `GET /api/v1/messages` takes `?channel=`, and `notCaught`
is gone. Nothing is invented where K8's behaviour on another channel is unobserved: no delivery report and no view of
its own, and no hold-back, since what K8 counts as a duplicate there has not been observed. `log:` and recipient objects
stay refused.

One message type keeps the read API a single list that a test filters, and a client's `to` is read the same way for
every channel. A type per channel and a list of their own would each give a test several shapes to read for one send,
while how K8 differs on those channels is unobserved. Naming the others leaves a request without an SMS recipient with
nothing to catch.

## Consequences

- **Good:** a client sending to any channel runs against the emulator, and what it sent can be read and tested.
- **Bad:** the read API changes shape, so a test reading SMS asks for `channel=sms`; what the emulator does for a
  channel other than SMS rests on the specification or on what K8 was seen to do for an SMS, and is listed in
  [k8-behaviour.md](../k8-behaviour.md).
- **Follow-up:** none.
