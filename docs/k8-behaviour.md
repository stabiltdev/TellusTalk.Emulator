# What K8 was seen to do

The emulator does what TellusTalk's K8 service was seen to do, and follows TellusTalk's
[published specification](https://k8api.tellustalk.com/openapi_docs/redoc.html) where nothing was seen. Each
observation carries its date, so a later change in TellusTalk's API can be told from an old observation. Numbers are
from PTS's range for fiction, and no account is named.

## Observed

### Credentials

- **2026-11-07:** a send without an `Authorization` header → `401` with `Content-Type: text/html; charset=utf-8`, no
  `WWW-Authenticate`, and the text "The server could not verify that you are authorized to access the URL requested.
  You either supplied the wrong credentials (e.g. a bad password), or your browser doesn't understand how to supply
  the credentials required." The answer carries the same headers as every other answer to a send.

### The answer to a send

- **2026-11-07:** a send of one SMS → `200` with `Content-Type: application/json`, no charset, and a body of
  `job_id`, `job_uuid`, `log_uuid` and `job_size`, where the three ids are the same value and `job_size` is the number
  of recipients.
- **2026-11-07:** any send → the headers `X-Request-Id`, `X-Content-Type-Options: nosniff`, `Strict-Transport-Security:
  max-age=31536000; includeSubDomains`, `Cross-Origin-Opener-Policy: same-origin`, `Referrer-Policy:
  strict-origin-when-cross-origin`, `Cross-Origin-Resource-Policy: same-origin` and `Permissions-Policy: geolocation=(),
  camera=(), microphone=()`, on refusals too.
- **2026-11-07:** a send → ids (`job_id`, `job_uuid`, `log_uuid`, `X-Request-Id` and a report's request id) that are
  version 1 UUIDs written as 32 lowercase hexadecimal characters without hyphens.

### Requests K8 accepts although the specification forbids them

- **2026-11-07:** a send with a field no schema defines → accepted, and the field is ignored.
- **2026-11-07:** a send with `"sms_flash": "true"`, a string where the specification has a boolean → accepted.
- **2026-11-07:** a send to a number written with spaces → accepted, and the spaces are removed.
- **2026-11-07:** a send with a bare e-mail address as `report_to` → accepted.
- **2026-11-07:** a send with a twelve-character sender name → accepted, and the name is passed on whole.

### Delivery reports

- **2026-11-07:** a delivered SMS with a URL in `report_to` → a `POST` of JSON with `log_status` `txtDELIVERED`,
  `log_status_code` `22`, an empty `failed_reason`, `article_id` `SMS_Sent`, `to_protocol` `SMS`, `to_address`
  `sms:<number>`, `from_address` `user_id:<user>`, the message text as `text`, `html` and `text_linebreaksbr`, `echo`
  as sent, `quantity` as the number of parts, the three ids, `report_time` and `template_optional_line`, in the order
  the README shows.
- **2026-11-07:** a failed SMS → the same report with `log_status` `txtDELIVERY_FAILED`, `log_status_code` `21` and
  `failed_reason` `txtFAILED_REASON: txtOPERATOR_ROUTING_ERROR, None`.
- **2026-11-07:** a report → the headers `Content-Type: application/json`, `Accept: */*`, and a `webpages-request-id` and
  an `x-webpages-request-id` with the same value, and every header named in the address's settings.

### The sender

- **2026-11-07:** a send without sender fields → the phone shows the account's configured sender, not the
  specification's `replysms` default.

### The user's Sender setting

TellusTalk's user setting Sender (Settings, SMS) offers Reply SMS, Number, Short code, Text and Subject. Reply SMS is
the default. The reply number is the two-way number TellusTalk assigns the account; it is no setting.

- **2026-11-14:** a send without sender fields, with the user's Sender at Reply SMS or at Subject → the phone shows the
  reply number.
- **2026-11-14:** a send without sender fields, with the user's Sender at Text → the phone shows that text.
- **2026-11-14:** a send without sender fields, with the user's Sender at Number → the phone shows that number.
- **2026-11-14:** `sms_originator_source: replysms` with the user's Sender at Reply SMS, and again at Text → the phone
  shows the reply number both times.

### Sender fields K8 refuses

Each refusal is a `400` with `Content-Type: text/html; charset=utf-8`, the same headers as every other answer, and the
text as the body without a trailing newline. K8 runs on Python, so a value after `got` or `string:` is Python's `str()`
of the parsed JSON value.

- **2026-11-14:** `subject`, `sms_originator_source`, `sms_originator_text` or `sms_originator_number` that is `null`
  → `Invalid <field> not a string: None`. `subject` was also null beside a source and its value, and the other three
  without one.
- **2026-11-14:** `subject: 123` → `Invalid subject not a string: 123`, and `sms_originator_number: 46701740699` beside
  source `text` and its value → `Invalid sms_originator_number not a string: 46701740699`.
- **2026-11-14:** `subject` that is `true`, `1.50`, `["Acme", 1, null]` or `{"name": "Acme", "on": true}` → `Invalid
  subject not a string: True`, `... 1.5`, `... ['Acme', 1, None]` and `... {'name': 'Acme', 'on': True}`.
- **2026-11-14:** an `sms_originator_source` that is none of `replysms`, `text`, `number` and `subject`, matched exactly
  (`Text` and the empty string included) → `Invalid sms_originator_source: <value>; must be one of
  frozenset({'replysms', 'number', 'text', 'subject'})`. A Python `frozenset` is ordered by hash randomisation per
  process, and the order was seen to vary between K8's servers; the emulator always gives the order seen twice.
- **2026-11-14:** source `subject` without `subject`, or with `""` → `400 Bad Request: Missing/invalid 'subject'.
  Expected non-empty string, got 'None'.` and `... got ''.`, the only text that ends in a period.
- **2026-11-14:** source `text` without `sms_originator_text`, or with `""` → `400 Bad Request: Missing/invalid
  'sms_originator_text'. Expected string, got 'None'` and `... got ''`.
- **2026-11-14:** source `number` without `sms_originator_number`, or with `""` → `400 Bad Request: Missing/invalid
  'sms_originator_number'. Expected number, got 'None'` and `... got ''`.
- **2026-11-14:** `sms_originator_number: ""` without a source, with the user's Sender at Reply SMS → accepted.

## Not observed

Where TellusTalk's behaviour has not been observed, the emulator follows its specification. These are the places; each
is settled when someone observes the real thing.

- What TellusTalk does when the text needs more parts than `sms_max_parts`: refuse, truncate or send.
- What `sms_character_set: gsm` does with a character outside the GSM alphabet.
- How `binary` expects its content, and how `sms_udh` and TellusTalk's own concatenation header share a part's room.
- Whether `sms_header` is joined to the text with a separator.
- What the short links that `redirect=` and `--LINK_VIEW--` become look like and how long they are, where a redirect's
  address ends, and whether a delivery report's `text` carries them; the emulator takes their form from the
  specification's examples and ends an address at whitespace.
- Which refusal K8 gives when a request breaks more than one sender rule, and how its sender refusals rank against what
  the emulator itself cannot record; the emulator checks the types, then the source, then the missing value.
- Every other type in each of the four sender fields, such as `true` or a list in `sms_originator_text`; the emulator
  refuses all of them as it refuses the observed cases.
- `null` or another type in any field other than the four sender fields; not every field is type-checked, since
  `"sms_flash": "true"` was accepted on 2026-11-07.
- How TellusTalk reads a `scheduled_send_time` without an offset; the emulator reads it as Swedish time.
- What `log_uuid` holds when a request has several recipients, and whether `job_size` counts a `group:` recipient as
  one or as each of its members; the emulator counts it as one.
- What a short code as the user's Sender shows; the emulator has no setting for it (issue #35).
- What `html` and `attachments` do on an SMS.
- What else TellusTalk refuses, apart from a missing `Authorization` and the sender fields above, and with what text.
- What K8 answers when it fails: the emulator's `4xx` and `5xx`, apart from `401`, are plain text with the reason
  phrase, as the specification shows them.
- What the phone shows for a sender name over 11 characters, and whether `"sms_flash": "true"` sends a flash SMS.
- How TellusTalk treats the other rules the warnings cover.
- What a reply contains, whether any report comes before the final one, and which other values a report's status
  takes.
- Whether TellusTalk sends a report again when the receiver fails, and what a report in another `content_type` holds.
