---
status: accepted
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-post-delivery-reports-only-to-http-urls: Post delivery reports only to the http(s) URLs a request names, with certificates checked

## Context and Problem Statement

An app that asks for delivery reports needs them to arrive to test that path at all. `report_to` takes an e-mail
address, an `sms:` number, a URL, or a URL carrying its own settings (`content_type` and headers). The emulator must
never send anything a request catches onward to a phone, a mailbox or TellusTalk, and only JSON reports have been seen.
A report to an app on the developer's machine, from inside a container, meets the ASP.NET Core development certificate,
which the container does not trust.

## Considered Options

- Post to every URL and accept any certificate, so a container can reach a development certificate over https.
- Post to http(s) URLs with the certificate checked, and record every other address as not sent.

## Decision Outcome

**A delivery report is posted only to an http(s) URL in `report_to`, as JSON in the form TellusTalk posts it, and the
receiver's certificate is checked; an e-mail or `sms:` address, or a `content_type` other than `application/json`, is
recorded on the message as not sent.** `AutomaticReports` sends one `ReportDelaySeconds` after a message is sent or falls
due, failed for a number in `FailingNumbers`; `POST api/v1/messages/{id}/reports` and the page's buttons send one on
request. Every attempt is kept on the message with its outcome; nothing is retried.

E-mail and SMS would leave the machine, and a report in a format nobody has seen would be invented. An `http://` address,
such as `http://host.docker.internal:<port>/…`, covers a development app without turning certificate checks off.

## Consequences

- **Good:** an app's report handling, its failure path included, can be tested locally.
- **Bad:** an https receiver whose certificate the emulator does not trust gets no report; the attempt records why.
- **Follow-up:** none.
