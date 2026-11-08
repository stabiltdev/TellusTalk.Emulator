---
status: accepted
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-know-every-field-of-every-send-schema: Know every field of every send schema, whatever the channel

## Context and Problem Statement

To warn about an unknown field, the emulator needs to know which fields are known. The specification's schemas cannot
be used as they stand: the one `additionalProperties: false`, on `sms_mms_params`, would forbid even `to` and `text`;
`send_to_list` defines no `sms_*` field; `send_sms_msg` has no `echo`, `scheduled_send_time` or `billing_id`; and the
specification's own multi-recipient example sends `subject` to an SMS recipient. The warning matters: TellusTalk
ignored a misspelt `sms_orginator_text` and sent the SMS from the account's sender.

## Considered Options

- Check a request against its channel's own schema, as the specification reads literally.
- Know every field that any send schema defines, for any channel.
- Keep no list and accept every field without a word.

## Decision Outcome

**A field is known when any of TellusTalk's send schemas defines it, for any channel; any other field is kept and
warned about as `unknown-field`.** `SendRequestBody` declares each known field as a `JsonElement` named with
`[JsonPropertyName]` and, where the schemas fix one, marked with the JSON shape it should have (`[Expects]`); a value of
another shape is warned about as `wrong-json-type`.

The union is the one reading under which every documented request is valid. Holding each field as a `JsonElement` is
what lets a wrong type stay a warning
([ADR 20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest](20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest.md)):
a typed deserialisation would throw on `"sms_flash": "true"`.

## Consequences

- **Good:** a misspelt field name shows up as a warning before TellusTalk ignores it in production.
- **Bad:** a field of another channel on an SMS, such as a fax parameter, is known and goes unwarned; `SendRequestBody`
  holds fields the emulator never reads, there only to be known.
- **Follow-up:** none.
