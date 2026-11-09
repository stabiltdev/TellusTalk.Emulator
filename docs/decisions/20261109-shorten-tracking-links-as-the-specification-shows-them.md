---
status: accepted
date: 2026-11-09
deciders: [Peter Hedberg]
---

# ADR 20261109-shorten-tracking-links-as-the-specification-shows-them: Shorten tracking links in an SMS text as the specification's examples show them

## Context and Problem Statement

An SMS text may carry `redirect=URL`, which TellusTalk replaces with a short link of its own that logs each click, and
`--LINK_VIEW--`, which it replaces with a link to the message's page at ebox.nu. The emulator showed the text as written,
so the phone showed a long address where the recipient sees a short one, and the part count, which depends on the
text's length, could show two parts for an SMS that is one in production. The short link has not been observed; only
the specification's examples show its form.

## Considered Options

- Show the text as written.
- Replace each with a link in the form of the specification's examples, with a placeholder id.
- Replace each with a working link to the emulator that redirects to the target.

## Decision Outcome

**Each `redirect=` followed by an http(s) URL, and each `--LINK_VIEW--`, in an SMS text with its header is replaced by
a link in the form of the specification's examples, `https://ebox.nu/r_xxxxxx/<n>` numbered from 0 and
`https://ebox.nu/z_xxxxxx`; the parts are counted on the replaced text, and the message lists each link with where it
leads.** `TrackingLinks` does the replacing; the read API serves the list as `links` and the page as "Links".

The part count is much of what the emulator is for, and it depends on the link's length. A link to the emulator would
count the parts on the emulator's address rather than TellusTalk's. The placeholder keeps the examples' length without
writing a link that could lead to someone else's message on TellusTalk's site, so the page shows the links as text,
never as anchors. The list keeps each target, so a test can still assert where a link leads once `text` no longer holds
it.

## Consequences

- **Good:** the phone and the parts match production as far as the specification's examples hold.
- **Bad:** `text` no longer holds the target address, so a test that asserted on it reads `links` or `fields.text`
  instead; and the short link's length rests on the examples, listed in the README until it is observed.
- **Follow-up:** none.
