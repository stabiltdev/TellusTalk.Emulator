---
status: accepted
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest: Accept what TellusTalk accepts and warn about what its specification forbids

## Context and Problem Statement

A stand-in is only worth having if a client that works against it works against TellusTalk, and the other way round.
TellusTalk's published specification forbids a good deal that the live service accepts: probed in November 2026, K8
accepted all five requests that broke the specification (an unknown field, `"sms_flash": "true"`, spaces in a number, a
bare e-mail address as `report_to` and a twelve-character sender). The first design read the specification strictly and
would have refused all five.

## Considered Options

- Refuse what the specification forbids.
- Accept what TellusTalk accepts, silently.
- Accept what TellusTalk accepts, and put a warning on the message for what the specification forbids.

## Decision Outcome

**The emulator refuses only what TellusTalk was seen to refuse and what it cannot record itself; everything else is
accepted and answered as TellusTalk answers it, and what the specification forbids becomes a warning on the message.**
A refusal that is the emulator's own limit says so: its `text/plain` body begins `TellusTalk Emulator:`. Warnings carry
stable codes (`WarningCodes`), appear on the page and in the read API, and a test can assert that a message carries
none.

A refusal TellusTalk would not give breaks a client that works in production. Accepting silently hides a mistake until
TellusTalk reads it in some way nobody has seen. A warning keeps the client working and still shows the mistake.

## Consequences

- **Good:** a client's mistakes show up locally, without failing where TellusTalk would not.
- **Bad:** the emulator has to read malformed requests without failing, so every field is held as raw JSON
  ([ADR 20261108-know-every-field-of-every-send-schema](20261108-know-every-field-of-every-send-schema.md)).
- **Follow-up:** a rule that rests on the specification rather than on observation is listed in the README under
  "Behaviour that follows the specification, not observation"
  ([ADR 20261108-follow-observed-behaviour-before-the-specification](20261108-follow-observed-behaviour-before-the-specification.md)).
