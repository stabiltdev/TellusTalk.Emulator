---
status: accepted
date: 2026-11-09
deciders: [Peter Hedberg]
---

# ADR 20261109-catch-the-sms-recipients-and-name-the-others: Catch a request's SMS recipients and name its others

## Context and Problem Statement

The specification's own example for several recipients sends one job to an `sms:` and an `email:` address. The
emulator catches SMS only, and it refused any request with a recipient on another channel, so a client sending that
job worked against TellusTalk and failed against the emulator: stricter than TellusTalk, which
[ADR 20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest](20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest.md)
allows only for what the emulator cannot record.

## Considered Options

- Refuse a request with any recipient on another channel.
- Catch the SMS recipients and warn about the others.
- Catch the SMS recipients and name the others on each caught message.
- Catch every channel.

## Decision Outcome

**A request's `sms:` recipients are caught; its `email:`, `fax:`, `mms:`, `voice:` and `group:` recipients are counted
in `job_size` and named on each caught message. A request with none of its recipients on `sms:`, and one with a `log:`
recipient, is refused as the emulator's limit.** The others are `SendRequest.NotCaught`, the read API's `notCaught` and
the page's "Not caught".

A warning says that a client broke the specification, and a mixed list is the specification's own example; a test
asserting that a message carries no warnings would fail for a correct request. Catching every channel means a stand-in
for e-mail, fax and voice besides SMS. A `log:` recipient makes TellusTalk answer with a `log_href` that the client waits
for and the emulator cannot give, and a request without an SMS recipient would leave nothing to show or test against,
so both stay refused.

## Consequences

- **Good:** a client sending one job to SMS and other channels works against the emulator, and its SMS can be read
  and tested.
- **Bad:** what goes to the other channels cannot be tested at all, and `job_size` counts a `group:` recipient as one,
  which is a reading of the specification's "recipient count" listed in the README.
- **Follow-up:** none.
