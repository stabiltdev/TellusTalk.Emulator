---
status: accepted
date: 2026-11-25
deciders: [Peter Hedberg]
supersedes: [ADR 20261109-shorten-tracking-links-as-the-specification-shows-them](20261109-shorten-tracking-links-as-the-specification-shows-them.md)
---

# ADR 20261125-send-redirect-and-the-view-token-as-written: Send redirect= and --LINK_VIEW-- in an SMS text as written, as K8 does

## Context and Problem Statement

[ADR 20261109-shorten-tracking-links-as-the-specification-shows-them](20261109-shorten-tracking-links-as-the-specification-shows-them.md)
replaced `redirect=URL` and `--LINK_VIEW--` with short links in the form of the specification's examples, which no one
had seen K8 send. On 2026-11-24 and 2026-11-25, a test user's texts with both reached a phone, the admin's log and the
reports exactly as written. Whether an account with tracking links turned on gets links, if any can, is not known.

## Considered Options

- Send them as written, as K8 was seen to.
- Keep shortening them, or shorten them behind a setting that is off by default.

## Decision Outcome

**`redirect=` and `--LINK_VIEW--` in an SMS text are sent, counted and shown as written.** The read API and the client
package have no `links`.

The emulator follows what K8 was seen to do
([ADR 20261114-record-each-observation-of-k8-with-its-date](20261114-record-each-observation-of-k8-with-its-date.md)).
A setting for links nobody has seen would rest on the specification's examples alone, and its form could not be
checked.

## Consequences

- **Good:** the phone, the parts and the duplicate key count the text K8 sends.
- **Bad:** the read API and the client package lose `links`, a breaking change for a test that read them; and an
  account that does get links, if there is one, is not emulated.
- **Follow-up:** ask TellusTalk whether tracking links need turning on, and what they then look like.
