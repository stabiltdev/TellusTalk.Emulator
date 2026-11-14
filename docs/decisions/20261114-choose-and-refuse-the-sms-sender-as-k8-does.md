---
status: accepted
date: 2026-11-14
deciders: [Peter Hedberg]
---

# ADR 20261114-choose-and-refuse-the-sms-sender-as-k8-does: Choose the SMS sender and refuse sender fields as K8 does

## Context and Problem Statement

Probes on 2026-11-14 ([k8-behaviour.md](../k8-behaviour.md)) showed that K8 shows the request's `subject` when a request
has one and names no `sms_originator_source`, which a client in production relies on, where the emulator showed the
account's sender. K8 also refuses a sender field that is `null` or not a string, an unknown source, and a source without
its value, where the emulator accepted all of them with at most a warning. A client that works against the emulator
could fail against K8, and the other way round.

## Considered Options

- Keep warning about these requests, and keep showing the account's sender when no source is named.
- Choose the sender and refuse the requests as K8 was seen to.

## Decision Outcome

**Without a source the first of `subject`, `sms_originator_text` and `sms_originator_number` that is present is the
sender, or the reply number when it is empty; with none of them the user's Sender setting decides. A sender field that
is not a string, an unknown source, and a source whose field is absent or empty are refused with K8's own `400`.**
`SenderResolver` chooses, and `sender.source` names the field that decided: the request's source when it named one, the
field K8 chose when it did not, `account` when the setting did. `SenderRefusals` checks, in this order, the type of the
four sender fields (a JSON `null` included), then the source, then the source's value. The body is K8's text with the
value rendered as Python's `str()` prints it (`PythonText`), as `text/html; charset=utf-8` with TellusTalk's headers;
nothing is recorded and no number failure is spent. The reader returns it as `RefusedByTellusTalk`, beside the
emulator's own `Refused`.

The refusals come after the body is parsed and before the emulator's own limits on `to` and recipients: where K8 was seen
to refuse, the client sees K8's answer. The order among the three rules, and against the emulator's own limits, is
unobserved and listed as such. Refusing `null` follows [ADR 20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest](20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest.md):
the emulator imitates TellusTalk, not what suits a client, and a client that sends `null` meets the same refusal in
production.

Warning was the weaker option because it lets a request pass that K8 refuses, so the client's own mistake surfaces only
in production.

## Consequences

- **Good:** a client sees the sender and the refusals it will see in production.
- **Bad:** `sender.source` can now be `subject`, `text` or `number` for a request without a source, a change to the read
  API's contract; the warnings `originator-missing` and `originator-source-invalid` are gone; a request with a `null`
  sender field, which a client's serializer may write for a field it leaves out, is refused. The type of every other
  field stays unchecked.
- **Follow-up:** which refusal wins when a request breaks several rules, and `null` in other fields, wait for a probe
  round.
