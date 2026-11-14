---
status: superseded by [ADR 20261114-record-each-observation-of-k8-with-its-date](20261114-record-each-observation-of-k8-with-its-date.md)
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-follow-observed-behaviour-before-the-specification: Follow TellusTalk's observed behaviour before its specification

## Context and Problem Statement

TellusTalk publishes an OpenAPI document for K8, but the live service departs from it: it accepts much the document
forbids, and a send without a sender shows the account's sender rather than the documented `replysms` default. Neither
document says what a delivery report contains. Some behaviour can be settled by probing a K8 account, much cannot, and
the legacy platform's reports turned out to differ entirely from K8's.

## Considered Options

- Follow the specification throughout.
- Follow what TellusTalk was observed to do, the specification where nothing was observed, and say which is which.

## Decision Outcome

**Where TellusTalk's live K8 service was observed, the emulator does what it did; elsewhere it follows the published
specification, and the README lists every such place under "Behaviour that follows the specification, not
observation".** Only K8 counts as evidence; the legacy platform does not. A delivery report is posted only in a format
TellusTalk was seen to post, field for field and in order, and a report format nobody has seen is not invented.

The point of the emulator is that a client behaves the same against it as against TellusTalk, and the live service is
what a client meets in production. Saying where the emulator rests on the document tells a user where it might differ.

## Consequences

- **Good:** a user knows which behaviour is proven and which is a reading of the document.
- **Bad:** the list of unobserved behaviour has to be kept current, and settling an item needs a K8 account to probe
  with.
- **Follow-up:** when an item is observed, change the behaviour and remove it from the README's list in the same change.
