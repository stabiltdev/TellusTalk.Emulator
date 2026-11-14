---
status: accepted
date: 2026-11-14
deciders: [Peter Hedberg]
---

# ADR 20261114-mirror-tellustalks-sender-setting-and-reply-number: Mirror TellusTalk's Sender setting and reply number in the settings

## Context and Problem Statement

What K8 shows for a request without sender fields comes from the user's Sender setting, which is Reply SMS, Number, Text
or Subject, and from a reply number TellusTalk assigns, shown for Reply SMS and Subject. The emulator had one setting,
`AccountSender`, for a name or a number, which cannot say which kind it holds or show the reply number beside a name.

## Considered Options

- One `AccountSender`, shown where K8 shows the reply number.
- Two settings: the user's Sender and the reply number.
- Two keys for the Sender, `Kind` and `Value`.
- Infer the kind from the value.

## Decision Outcome

**`Emulator__SmsSender` takes `replysms`, `text:<name>`, `number:<number>` or `subject`, in the words of a request's
`sms_originator_source`, and defaults to `replysms`; `Emulator__ReplyNumber` is the reply number and defaults to
`+46701740698`, a fresh account's behaviour.** `SenderSettings` parses them at startup, and a value that does not parse
stops the emulator with a message naming the variable and the forms. `Emulator__AccountSender` is obsolete and honoured
only when `SmsSender` is not set: `+` and digits mean `replysms` with that number as the reply number unless
`ReplyNumber` is set, anything else means `text:<value>`. Both packages write the same variables through `SmsSender`,
`WithSmsSender` and `WithReplyNumber`; `WithAccountSender` is obsolete and still writes its own variable, so the server
holds the one translation.

One key is one row in the README and the flat style of `Emulator__FailingNumbers`, where two keys leave a `Value` that
means nothing for `replysms` and `subject`. Inferring the kind cannot express Subject or Reply SMS. A separate reply
number lets the emulator carry the behaviour as observed instead of leaving it out.

## Consequences

- **Good:** the settings read as TellusTalk's admin does, and existing configurations keep working.
- **Bad:** an emulator nobody configured shows `+46701740698` where it showed `Emulator`; two settings and an alias to
  keep until the alias is removed.
- **Follow-up:** short code as the user's Sender, and per-user settings, wait for observation and a later step.
