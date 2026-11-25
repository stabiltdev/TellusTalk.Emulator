---
status: accepted
date: 2026-11-25
deciders: [Peter Hedberg]
---

# ADR 20261125-mirror-tellustalks-max-number-of-parts: Mirror TellusTalk's Max number of parts in the settings

## Context and Problem Statement

A TellusTalk user has a setting Max number of parts, under Mail → SMS, which defaults to the organisation's 8. K8 was
seen to cut a text sent through the API without `sms_max_parts` to what fits it, and to let `sms_max_parts` hold over it
whichever way the two differ. The emulator used the specification's default of 8 alone, so a test could not try how
the user's setting cuts a message.

## Considered Options

- A setting at the emulator's level, beside the user's Sender setting.
- No setting: a test sends `sms_max_parts` to try a cut message.

## Decision Outcome

**`Emulator__SmsMaxParts`, 8 by default and 1 or more, is the user's Max number of parts: it limits a text sent
without `sms_max_parts`, or with one below 1.** `MessageFactory` takes `sms_max_parts` when it is 1 or more, else the
setting; `WithSmsMaxParts` writes it in both packages.

An app under test may never send `sms_max_parts` and still have its messages cut by the account it runs against, so
the setting is the only way to try that. It sits at the emulator's level, as `Emulator__SmsSender` does, until the
emulator has users of its own. The name follows `SmsSender` and the request's `sms_max_parts`, not the admin's label.

## Consequences

- **Good:** a test can set the account's limit and see a message cut, as K8 cuts it.
- **Bad:** one more setting in the contract the packages write.
- **Follow-up:** none.
