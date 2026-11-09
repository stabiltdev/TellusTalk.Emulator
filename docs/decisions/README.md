# Decisions

Short, dated records of decisions that shape the emulator: what was faced, what was considered, what was chosen, and
what it costs. Read them before changing something that looks odd; most of the odd things are on purpose.

## When to write one

**Write a record when a change involves:**

1. A new dependency, service or external integration
2. A change to what other repositories depend on: the read API's shape, the image, the settings
3. A behaviour of TellusTalk's that the emulator follows, or deliberately does not
4. A pattern the code will now repeat
5. A choice made deliberately against the obvious option

**Do not write one for** version bumps, naming, formatting, anything one pull request can reverse without consequence,
or anything a record already covers. [conventions.md](../conventions.md) carries the conventions.

## Writing one

Copy [template.md](template.md) to `YYYYMMDD-short-title-with-dashes.md`, dated the day you create the file. The file
name without `.md` is the record's identity, and the heading opens with it:
`# ADR 20261108-mint-version-1-ids-as-tellustalk-does: Mint version 1 UUIDs as TellusTalk does`. Title the record with
the decision itself, as the rule that holds, and add its row to the table below in the same change.

Cite a record as a link with its identity as the text, such as
[ADR 20261108-mint-version-1-ids-as-tellustalk-does](20261108-mint-version-1-ids-as-tellustalk-does.md); in a code
comment, without the link.

## Superseding

An accepted record is not edited. When a decision changes, write a new record whose front matter says
`supersedes: [ADR <identity>](<identity>.md)` and whose context opens with what changed; in the old record, change only
its `status` to `superseded by [ADR <identity>](<identity>.md)`, and update its row here.

## Records

| Record | Decision | Status |
|---|---|---|
| [20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest](20261108-accept-what-tellustalk-accepts-and-warn-about-the-rest.md) | Accept what TellusTalk accepts and warn about what its specification forbids | accepted |
| [20261108-know-every-field-of-every-send-schema](20261108-know-every-field-of-every-send-schema.md) | Know every field of every send schema, whatever the channel | accepted |
| [20261108-follow-observed-behaviour-before-the-specification](20261108-follow-observed-behaviour-before-the-specification.md) | Follow TellusTalk's observed behaviour before its specification | accepted |
| [20261108-mint-version-1-ids-as-tellustalk-does](20261108-mint-version-1-ids-as-tellustalk-does.md) | Mint version 1 UUIDs as TellusTalk does | accepted |
| [20261108-read-a-time-without-an-offset-as-swedish-time](20261108-read-a-time-without-an-offset-as-swedish-time.md) | Read a scheduled time without an offset as Swedish time | accepted |
| [20261108-post-delivery-reports-only-to-http-urls](20261108-post-delivery-reports-only-to-http-urls.md) | Post delivery reports only to the http(s) URLs a request names, with certificates checked | accepted |
| [20261108-render-the-page-on-the-server-and-swap-it-in-place](20261108-render-the-page-on-the-server-and-swap-it-in-place.md) | Render the page on the server and swap its parts in place | accepted |
| [20261108-serve-tellustalks-paths-at-the-root](20261108-serve-tellustalks-paths-at-the-root.md) | Serve TellusTalk's paths at the root, as TellusTalk serves them | accepted |
| [20261108-ship-an-aspire-package-of-the-images-version](20261108-ship-an-aspire-package-of-the-images-version.md) | Ship an Aspire package that runs the image of its own version | accepted |
| [20261109-catch-the-sms-recipients-and-name-the-others](20261109-catch-the-sms-recipients-and-name-the-others.md) | Catch a request's SMS recipients and name its others | accepted |
| [20261109-shorten-tracking-links-as-the-specification-shows-them](20261109-shorten-tracking-links-as-the-specification-shows-them.md) | Shorten tracking links in an SMS text as the specification's examples show them | accepted |
| [20261109-fail-a-counted-number-of-sends-on-request](20261109-fail-a-counted-number-of-sends-on-request.md) | Fail a counted number of sends when a test asks for it | superseded by [20261109-fail-a-counted-number-of-sends-late-if-asked](20261109-fail-a-counted-number-of-sends-late-if-asked.md) |
| [20261109-fail-a-counted-number-of-sends-late-if-asked](20261109-fail-a-counted-number-of-sends-late-if-asked.md) | Fail a counted number of sends with a 4xx or 5xx, late if a test asks | accepted |
