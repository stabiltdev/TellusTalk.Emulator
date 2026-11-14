---
status: accepted
date: 2026-11-14
deciders: [Peter Hedberg]
---

# ADR 20261114-hold-back-a-duplicate-as-k8-does: Hold back a duplicate send as K8 does

## Context and Problem Statement

K8 holds back an SMS it takes for a duplicate of one the same user sent in the five minutes before: the send is answered
`200` as usual, nothing reaches the phone, and `report_to` gets a blocked report at once (see
[k8-behaviour.md](../k8-behaviour.md)). The emulator caught every send, so a test that sent the same SMS twice saw two
messages where production delivers one, and an app never met the blocked report. A test suite that repeats a message on
purpose still needs the emulator to keep both, and the read API's JSON, which other repositories test against, has to
show a held-back message without a test counting it as sent.

## Considered Options

Whether a duplicate is held back:

- Catch every send.
- Hold back a duplicate always.
- Hold back a duplicate by default, with `Emulator__BlockDuplicates` to turn it off.

Where the rule lives:

- `MessageStore.Add` decides, under the lock it already holds.
- A separate guard with a memory of its own, asked before the messages are stored.
- `MessageFactory` checks with a query against the store.

How the read API shows a held-back message:

- It stays `sent` and carries a marker.
- It has a state of its own, `blocked`, and a `duplicateOf` field.

## Decision Outcome

**A send that duplicates one the same user made within five minutes is held back by default, and the read API gives it
the state `blocked`.** `MessageStore.Add` compares each new message, under the lock it already holds, with the messages
that were not held back and were received within five minutes either side, the boundary included. The key is the user,
the normalised number, `subject` (absent and `""` taken as the same) and `text`, compared ordinally. A held-back message
records the id of the message it duplicates as `DuplicateOf`, is reported at once with the status `Blocked`, and never
falls due. The answer stays `200`. `Emulator__BlockDuplicates` turns the holding back off, and both packages set it
through `WithDuplicateBlocking(bool enabled)`; with it off the read API keeps its shape, with `duplicateOf` `null`.

Catching every send leaves the emulator unlike the service an app meets in production, and holding back always leaves a
suite that repeats a message with no way to do so. A state of its own keeps `?state=sent` to what reached a phone, where
a marker on a sent message would have a test counting what was sent to a number count the held-back one. The deciding
comparison sits in the store because check and add must be one step under one lock, so two identical simultaneous sends
cannot both pass, and a test that clears the emulator also clears what could hold a later send back. A separate guard
would need a lock of its own across check and add, and would have to be told about every clearing and deleting; a check
in `MessageFactory` would make check and add two steps.

## Consequences

- **Good:** an app meets the blocked report and the one-message outcome it meets in production, and a test that asks for
  `state=sent` counts what was delivered.
- **Bad:** a test that sends the same text to the same number twice within five minutes meets a held-back message, so
  it gives each send a distinct text or turns the setting off. A client that reads `state` into a strict enum meets a
  value it did not know, which the 1.0.0 contract is the time for.
- **Follow-up:** the points of K8's behaviour not observed are listed in `docs/k8-behaviour.md`; each is settled when
  someone observes it.
