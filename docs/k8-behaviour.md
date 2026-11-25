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
- **2026-11-17:** a send to two SMS numbers, and to three → `job_id`, `job_uuid` and `job_size` (`2`, `3`), without
  `log_uuid`. The admin's log had a row per recipient, each with a log id of its own other than the job's; with one
  recipient the log id was the job's.
- **2026-11-17:** `sms:+4670174062X` → `{"job_id":…,"job_uuid":…,"log_uuid":…,"invalid_recipients":[{"address":"sms:+4670174062X","error":"Number Not Numeric"}],"job_size":1}`,
  the address as sent, its `+` unescaped. The admin's log showed it Blocked (1) with `failed_reason`
  `txtNUMBER_NOT_NUMERIC`, and nothing was sent.
- **2026-11-24:** `sms:+` and `sms:` → `200` with `invalid_recipients` `[{"address":"sms:+","error":"Not a Number"}]`
  and `[{"address":"sms:","error":"Missing Address"}]`, `job_size` `1`. The admin's log showed each Blocked (1), with
  `failed_reason` `txtNOT_A_NUMBER` and `txtMISSING_ADDRESS`, `failed_details` the address, and an empty `to_address`.
- **2026-11-24:** `sms:0701740660`, `0701740661` without a prefix, and `sms:46701740662` → accepted, and the admin's log
  showed them as `sms:+46701740660`, `sms:+46701740661` and `sms:+46701740662`, with the user's country code for local
  messages empty; with it at `46`, a local number became the same.
- **2026-11-17:** `report_to: "not an address"` →
  `{"job_id":…,"job_uuid":…,"invalid_report_to":[{"address":"not an address","error":"Unknown Address Type"}],"log_uuid":…,"job_size":1}`,
  and the message went out; `reply_to: "not an address"` alike, with `invalid_reply_to`.
- **2026-11-17:** a `group:` recipient in seven forms (`box.` and an address book's id, a label, the id alone, the
  address book's alias, its name, `template.` and a template's id, and the template's whole address) → each `200` with
  `job_size` `1` and `log_uuid` the job's id, logged once as Awaiting Send (10); nothing reached a member.

### Requests K8 accepts that the specification forbids

- **2026-11-07:** a send with a field no schema defines → accepted, and the field is ignored.
- **2026-11-07:** a send with `"sms_flash": "true"`, a string where the specification has a boolean → accepted.
- **2026-11-07:** a send to a number written with spaces → accepted, and the spaces are removed.
- **2026-11-07:** a send with a bare e-mail address as `report_to` → accepted.
- **2026-11-07:** a send with a twelve-character sender name → accepted, and the name is passed on whole.
- **2026-11-17:** `null` in `html`, `echo`, `scheduled_send_time`, `billing_id`, `sms_flash`, `sms_single_thread`,
  `sms_format`, `sms_character_set` and `sms_udh` → accepted.
- **2026-11-17:** another type than the specification's in `html` (`123`), `scheduled_send_time` (`1763400000`, sent at
  once), `billing_id` (`123`), `sms_max_parts` (`"2"`), `sms_flash` (`1`), `sms_validity_time` (`"60"`),
  `sms_single_thread` (`"true"`) and `sms_udh` (`123`) → accepted.
- **2026-11-17:** a number without a prefix, `+46701740620` → accepted and stored as `sms:+46701740620`; a number
  written with hyphens, `sms:+46-70-174-06-21` → accepted and stored without them.
- **2026-11-17:** a 14-character `subject` as the sender (passed on whole), `sms_single_thread` beside a text sender,
  `sms_format: "gsm"`, a binary SMS whose text is not hex, an emoji under `sms_character_set: gsm`, `sms_max_parts` of
  `0` and `-1`, a text of three parts under `sms_max_parts: 1` (stored whole), and a `scheduled_send_time` that is a word
  or not ISO 8601 (sent at once) → accepted.

### Parts

- **2026-11-24:** a text of 160 GSM characters, and one of 161 → reports with `quantity` `1` and `2`.
- **2026-11-24:** a text of 320 GSM characters under `sms_max_parts: 1` → `200` with nothing to show the limit, and a
  report whose `text` was the first 160 characters, `quantity` `1`; the admin's log kept the text whole.
- **2026-11-24:** the same text without `sms_max_parts`, with the user's Max number of parts (Settings, SMS, Mail → SMS)
  at 1 → the same, and a real phone got one SMS of the first 160 characters. The setting's default is the
  organisation's, 8; `Emulator__SmsMaxParts` mirrors it.
- **2026-11-24:** a text of 100 characters with `ł`, under `sms_character_set: gsm` and under `optimize` → reports with
  `quantity` `2` both, and `ł` kept in their `text`. So under `gsm` too, a character outside the GSM alphabet sends the
  text as UCS-2.
- **2026-11-25:** `ł` under `sms_character_set: gsm` → a real phone showed `ł`.
- **2026-11-25:** a text of 320 GSM characters under `sms_max_parts: 3`, with the user's Max number of parts at 1 → a
  report with the whole text and `quantity` `3`; under `sms_max_parts: 1`, with the setting at 3 → the first 160
  characters and `quantity` `1`. `sms_max_parts` holds either way, and the setting applies only without it.

### The header

- **2026-11-24:** `sms_header: "H"` beside a text of 159 characters, and of 160 → reports with `quantity` `2` both, and
  `text` the text alone; the admin's log had the text alone too.
- **2026-11-25:** `sms_header: "Header"` beside `Probe T23 body.` → a real phone showed the header, a blank line and the
  text. `sms_header: "H"` beside 158 characters → `quantity` `2`, so the header and the text are joined by two
  characters, `\n\n`.

### Tracking links

- **2026-11-24:** a text with `redirect=` and an https URL, twice, and one with `--LINK_VIEW--` → the admin's log and
  the reports had each text as sent.
- **2026-11-25:** both in one text, from a test user → a real phone showed the text exactly as sent, with no link in
  place of either.

### Scheduled sends

- **2026-11-24:** three sends scheduled for the same wall time, one without an offset, one with `Z` and one with
  `+01:00` → the one without an offset went out with the `Z` one, an hour after the `+01:00` one. K8 reads a time
  without an offset as UTC. The admin's log showed `scheduled_time` empty for all three; their report times showed
  when each went out.

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
- **2026-11-14:** a send with a `subject` and a URL in `report_to` → a report whose `text` is the subject, a blank line
  and the message, while the phone shows only the message; `html` and `text_linebreaksbr` write each line break as
  `<br>` followed by the line break. The other fields and the headers were as on 2026-11-07, with a `User-Agent` of
  Python's aiohttp.
- **2026-11-14:** a failed SMS → its report about a second after the send; a delivered one → after five to eight
  seconds.
- **2026-11-24:** a report address whose settings name `Authorization: Report probe-sh1`, `Report-Secret` and
  `X-Probe-MiXeD` → a report with all three and their values as written, beside K8's own headers. The names' case could
  not be seen, since the receiver was reached through a proxy that rewrites them.
- **2026-11-24:** a report → a row of its own in the admin's log: article HTTP Sent, `to_address` the URL, subject
  "Delivery Report", the send's `job_uuid` and a `log_uuid` of its own.
- **2026-11-24:** a receiver that answers `500`, or `401` → the report failed at once, and the admin's log showed
  Delivery Failed (21) with `failed_reason` `HTTPError: Error Code: 500` and `reason: Internal Server Error` on a second
  line, or `401` and `Unauthorized`. Nothing was sent again within two and a half hours.
- **2026-11-24:** a receiver that does not answer → the report failed after 60 seconds, with `failed_reason`
  `RequestError: Internal request timed out, url: ` and the URL. Nothing was sent again.
- **2026-11-24:** a receiver that answers `302` → K8 followed it with a `GET` without the report; one that answers
  `307` → the same `POST` with the report. Both were logged Delivery Success (22) on the target's `200`.
- **2026-11-25:** a receiver that answers `204 No Content` → the report failed, and the admin's log showed Delivery
  Failed (21) with `failed_reason` `HTTPError: Error Code: 204` and `reason: No Content` on a second line. Nothing was
  sent again within an hour.
- **2026-11-24:** an SMS with an `html` of its own → a report whose `html` was the text, not the request's `html`.
- **2026-11-24:** an SMS with `attachments` of one file → accepted; the report's `text` was the text alone, `quantity`
  `1`, and the admin's log listed the attachment's name.
- **2026-11-25:** an SMS with an `html` of its own and an attachment → a real phone showed the text alone.

### The user's report recipients

As of 2026-11-14, TellusTalk's user setting SMS Report Recipients (Settings, SMS) holds rows of a kind (Email, HTTP,
HTTPS, SMS, MMS, Fax or SMPP), a "when" (Never, Always, When Success, When Failed, Always with message, Delivered with
message or Failed with message) and an address, and one language for them all.

- **2026-11-14:** sends to `send/v1`, delivered and failed, with and without `report_to` → no report on any of eight
  rows: HTTPS at each of the seven "when", and Email at Always. A `report_to` URL got its report as usual.
- **2026-11-14:** an SMS sent from TellusTalk's own Send page by the same user → a report on the HTTPS row at Always and
  an e-mail on the Email row. The emulator catches only `send/v1`, so it has no report recipients of its own.
- **2026-11-14:** the language at English → the same JSON report to a `report_to` URL as at the default, whose e-mail
  report was in Swedish.

### The sender

- **2026-11-07:** a send without sender fields → the phone did not show the specification's `replysms` default; it
  showed what the account's Sender setting said (see the user's Sender setting below).

### The user's Sender setting

As of 2026-11-14, TellusTalk's user setting Sender (Settings, SMS) offers Reply SMS, Number, Short code, Text and
Subject. Reply SMS is the default. The reply number is the two-way number TellusTalk assigns the account; it is no setting.

- **2026-11-14:** a send without sender fields, with the user's Sender at Reply SMS or at Subject → the phone shows the
  reply number.
- **2026-11-14:** a send without sender fields, with the user's Sender at Text → the phone shows that text.
- **2026-11-14:** a send without sender fields, with the user's Sender at Number → the phone shows that number.
- **2026-11-14:** `sms_originator_source: replysms` with the user's Sender at Reply SMS, and again at Text → the phone
  shows the reply number both times.
- **2026-11-14:** source `subject` beside `subject`, with the user's Sender at Reply SMS → the phone shows the subject.

### Choosing the sender without a source

Observed with the user's Sender at Reply SMS unless it says otherwise.

- **2026-11-14:** `subject` alone → the phone shows the subject. `sms_originator_text` alone shows that text, also with
  the user's Sender at Text, and `sms_originator_number` alone shows that number.
- **2026-11-14:** `subject` beside `sms_originator_text`, and all three fields → the phone shows the subject.
  `sms_originator_text` beside `sms_originator_number` → it shows the text.
- **2026-11-14:** `subject: ""` with the user's Sender at Text → the phone shows the reply number. So it did for
  `sms_originator_text: ""` beside `sms_originator_number` and for `subject: ""` beside `sms_originator_text`, with the
  Sender probably at Text, and for `sms_originator_number: ""` alone with the Sender at Reply SMS. The empty field takes
  its place in the order and then shows the reply number.
- **2026-11-17:** `sms_originator_number: "Acme"` alone → accepted; the admin's log showed the source `number` and the
  account's reply number as the sender.

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
- **2026-11-14:** `sms_originator_source` that is `123` or `true`, `sms_originator_text` that is `true` or `["Acme"]`,
  and `sms_originator_number` that is `true` or `{"number": "+46701740699"}` → `Invalid <field> not a string: 123`,
  `... True`, `... True`, `... ['Acme']`, `... True` and `... {'number': '+46701740699'}`.
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

### Fields K8 refuses

Each refusal is a `400` and each server error a `500`, both with `Content-Type: text/html; charset=utf-8` and the text
as the body; the other headers were not recorded. A value in a text is Python's `str()` of the parsed JSON value, as
for the sender fields.

- **2026-11-17:** `to` that is `null`, or no `to` → `key: to is missing or empty`; `to: 46701740619` → `key: to is of
  type <class 'int'> expected (str,dict,list)`.
- **2026-11-17:** `text` that is `null` or `""`, or no `text` → `key: text is missing or empty`; `text: 123` → `key:
  text is of type int expected (str)`, without the `<class …>` of the text for `to`.
- **2026-11-17:** `report_to` that is `null` or `123` → `400 Bad Request: report_to is of type <class 'NoneType'>.
  Expected (list,dict,str)` and `... <class 'int'>. Expected (list,dict,str)`; `reply_to` alike, named `reply_to`.
- **2026-11-17:** `sms_validity_time: null` and `sms_max_parts: null` → `Invalid sms_validity_time not an int: None` and
  `Invalid sms_max_parts not an int: None`.
- **2026-11-17:** `sms_header` that is `null` or `123` → `Invalid sms_header not a string: None` and `... 123`.
- **2026-11-17:** `sms_character_set: 1`, and `sms_format: 1` without `sms_character_set` → `Invalid sms_character_set
  not a string: 1` both times.
- **2026-11-17:** `sms_character_set: "latin1"` → `Invalid sms_character_set: latin1; must be one of
  frozenset({'optimize', 'unicode', 'binary', 'gsm'})`.
- **2026-11-17:** `sms_character_set: "binary"` without `sms_udh` → `400 Bad Request: Binary sms_format missing
  sms_udh`.
- **2026-11-17:** `billing_id: "billing id-1"` → `Invalid billing_id billing id-1`.
- **2026-11-17:** `attachments: null` → `500` with `'NoneType' object is not iterable`, and `attachments: "file"` →
  `500` with `'str' object has no attribute 'pop'`. Neither was in the admin's log.
- **2026-11-24:** `attachments: ["file"]` → `500` with `'str' object has no attribute 'pop'`, and `attachments: 1` →
  `500` with `'int' object is not iterable`, neither in the admin's log; `attachments: {}` → accepted and sent, though
  the admin's log answered a 500 page for that row's details and left it out of an export.
- **2026-11-17:** a request that breaks two of these rules → the refusal of the first in these orders: `attachments`
  before `sms_validity_time`, `sms_max_parts`, `sms_header`, `reply_to`, `report_to` and `sms_character_set`;
  `reply_to` before `report_to` before `sms_character_set` before `sms_header`; `sms_validity_time` before
  `sms_max_parts` before `sms_header`.
- **2026-11-17:** each refusal of these fields but `billing_id`'s → a row in the admin's log, Blocked (1), with the
  refusal's text as `failed_reason`. The refusal of `billing_id` and the `500`s had no row.

### Duplicates

Observed with fresh test users sending to numbers from PTS's range for fiction, each with `report_to` on a local
receiver. A send that went out reported `txtDELIVERY_FAILED` within about three seconds; a send held back reported
`txtBLOCKED` within half a second.

- **2026-11-14:** an SMS identical to one the same user sent 10 seconds before → held back. The same text to another
  number, or with a `subject` where the first had none, went out; the text in lower case, or with a trailing space, went
  out too. The same text to the same number written `+46 70 174 06 08` was held back. So the key includes the user, the
  recipient's number as K8 normalises it, `subject`, and `text` as sent, compared exactly.
- **2026-11-14:** another `sms_originator_text`, `echo`, `report_to`, `sms_header`, `html`, `sms_flash`, `sms_max_parts`,
  `sms_validity_time`, `reply_to`, `billing_id` or `sms_single_thread` → each held back, so none of them is part of the
  key. The report of a held-back send with another `report_to` went to that send's own `report_to`.
- **2026-11-14:** the same request from another user of the same organisation → went out.
- **2026-11-14:** the same number twice in one request's `to` → the second held back as a duplicate of the first.
- **2026-11-14:** an identical send 4 minutes 40 seconds after the first → held back; 5 minutes 20 seconds after → went
  out. A send 5 minutes 21 seconds after the first that went out and 4 minutes 39 seconds after the last held back also
  went out, so the window counts back from the request's arrival against sends that were not held back themselves.
- **2026-11-14:** an identical send at once, 7 seconds after a send scheduled 4 minutes ahead and 5 seconds after one
  scheduled 11 minutes ahead → each held back. Both scheduled sends went out when they fell due. A scheduled send
  counts from when K8 received it.
- **2026-11-14:** the answer to a held-back send → `200` with `job_id`, `job_uuid`, `log_uuid` and `job_size`, as for any
  send.
- **2026-11-14:** the report of a held-back send → the report of any send with `log_status` `txtBLOCKED`,
  `log_status_code` `1`, `failed_reason` `txtFAILED_REASON: txtDUPLICATE_MESSAGE, Duplicate of message <id> sent within
  5 minutes.`, `failed_details` `Duplicate of message <id> sent within 5 minutes.` and `report_time` `null`, where
  `<id>` is the `log_uuid` of the send it duplicates. The other fields and the headers were those of every report.

## Not observed

Where TellusTalk's behaviour has not been observed, the emulator follows its specification. These are the places; each
is settled when someone observes the real thing.

- How K8 cuts a text to more than one part, and binary content; the emulator keeps the parts as the text splits, and
  binary content whole.
- How `binary` expects its content, and how `sms_udh` and TellusTalk's own concatenation header share a part's room.
- How K8 cuts a text with an `sms_header` to `sms_max_parts`, and whether `sms_header: ""` adds the blank line; the
  emulator cuts the header, the blank line and the text as one, and adds nothing for an empty header.
- Whether `redirect=` and `--LINK_VIEW--` become links for an account that has tracking links turned on, if any can,
  and what the links then look like; the emulator sends them as written, as a test user's SMS had them.
- Which refusal K8 gives when a request breaks more than one sender rule, and how its sender refusals rank against what
  the emulator itself cannot record; the emulator checks the types, then the source, then the missing value.
- The types not yet tried in a given sender field, such as a decimal in `sms_originator_text` or a list in
  `sms_originator_source`; each field was seen refusing several types that are not strings, with the same text, so the
  emulator refuses every such type in all four.
- Which refusal K8 gives when a request breaks two field rules not seen against each other, such as `to` against
  `attachments` or `report_to` against `sms_validity_time`, and how the field rules rank against the sender fields and
  `billing_id`; the emulator checks `to`, `text`, `attachments`, `reply_to`, `report_to`, `sms_validity_time`,
  `sms_max_parts`, the character set, `sms_header`, the sender fields and `billing_id` in that order, which keeps every
  order seen.
- Whether `0`, `false`, `[]` and `{}` are missing or empty in `to` and `text`; K8's text reads as Python's truth test,
  so the emulator refuses them as missing or empty, and gives the type text only to a value that is true.
- What `attachments` does beyond the values above. The emulator reads K8's texts as Python iterating the value and
  calling `pop` on each item: a number or a boolean fails as not iterable; a non-empty string or object fails on its
  first character or key, and a list on its first item that is a string, number, boolean or `null`; `[]`, `{}`, `""`
  and a list of objects or lists pass.
- The types not tried in the fields above: a decimal or a boolean in `to`, a list or an object in `text` or
  `sms_header`, a decimal or a boolean in `report_to` and `reply_to`, and in the counts any value but `null`, `"2"` and
  `"60"`. The emulator refuses every type but a string in `text` and `sms_header`, every value but a string, a list or
  an object in `to`, `report_to` and `reply_to`, and only `null` in the counts.
- Whether the refusals and server errors of 2026-11-17 carry the headers of every other answer; the emulator gives
  them those headers, as K8's sender refusals were seen to carry them.
- Whether `sms_format` stands in for an `sms_character_set` that is `null`, as it does for one that is absent; the
  emulator lets it.
- What `sms_format` does beside an `sms_character_set` that is set; the emulator ignores it, whatever its value, apart
  from the `format-deprecated` warning.
- Whether a character set is matched exactly, as a sender source is; the emulator refuses `"GSM"` and `""`.
- Whether the character sets in K8's refusal come in another order on another of K8's servers, as the sender sources
  do; the emulator gives the order seen.
- Whether a binary SMS whose `sms_udh` is `""` is refused as missing, and one whose `sms_udh` is `0`, `false`, `[]` or
  `{}`; the emulator refuses them all, by the truth test.
- Whether `billing_id: ""` is accepted, and which characters besides a space and a hyphen K8 refuses; the emulator
  accepts an empty id and refuses any character but ASCII letters and digits, `å` included.
- Whether a `billing_id` that is no string, such as `true` or a list, is accepted as `123` was; the emulator accepts it.
- What a short code as the user's Sender shows; the emulator has no setting for it (issue #35).
- What a report's `text` is for an empty `subject`; the emulator takes it as no subject.
- What else TellusTalk refuses, apart from a missing `Authorization`, the sender fields and the fields above, and with
  what text.
- What K8 answers when it fails for a reason of its own: the `4xx` and `5xx` a test arms are plain text with the reason
  phrase, as the specification shows them.
- What the phone shows for a sender name over 11 characters, and whether `"sms_flash": "true"` sends a flash SMS.
- How TellusTalk treats the other rules the warnings cover.
- What a reply contains, whether any report comes before the final one, and which other values a report's status
  takes.
- What a report in another `content_type` holds.
- Whether an absent `subject` and `subject: ""` count as the same in the key of a duplicate; the emulator takes them as
  the same.
- Duplicates on channels other than SMS, and what their key is; a log of 2026-11-14 showed an e-mail held back 36 seconds
  after the first. The emulator catches every channel but holds back SMS only, since the key on another channel has not
  been observed.
- What K8 answers a request whose recipients are all on channels other than SMS; the emulator answers as for an SMS.
- Whether K8 sends delivery reports for channels other than SMS, and in what form; the emulator reports SMS only.
- Whether K8 removes spaces and hyphens in a `fax:`, `mms:` or `voice:` number, and whether it blocks one with a
  letter; the emulator removes them as from an SMS number, and keeps a letter with the `to-not-digits` warning.
- How K8 reads a `to` address without a prefix that is no number, such as a bare e-mail address, and whether it blocks
  one; the emulator reads it as an SMS number and never blocks it, though it blocks the same address after `sms:`.
- How K8 reads digits without `+` that begin with neither a single `0` nor `46`, such as `0046…` or another country's
  code, and whether a country code for local messages other than Sweden's changes a local number; the emulator keeps
  such a number as written, with the `to-not-digits` warning.
- Where `invalid_recipients`, `invalid_report_to` and `invalid_reply_to` sit in an answer to several recipients, and
  in which order the two address lists come when a request has both; the emulator keeps the places seen with one
  recipient and writes `invalid_report_to` first.
- Which `report_to` and `reply_to` addresses K8 lists besides one that is no address at all, such as an `sms:` number
  with a letter or a URL without a host, and with what error; the emulator lists every one that is no e-mail address,
  `sms:` number or http(s) URL, with `Unknown Address Type`.
- How K8 writes characters outside ASCII in an answer's addresses; the emulator writes a letter such as `å` as it is,
  as K8 was seen to write a `+`, and escapes some others as `\u` sequences, among them an emoji and a space other than
  U+0020, such as U+00A0.
- Whether a number K8 blocks counts for the duplicate rule, as an original or as a duplicate; the emulator does neither.
- Whether a number K8 blocks gets a delivery report, and in what form; the emulator sends none (issue #31).
- What a local number such as `0701740698` as `sms_originator_number` shows as the sender, and what `"Acme"` shows
  beside a named source `number`; the emulator shows the reply number for every `number` that is not `+` and digits.
  On 2026-11-24 the admin's log showed `0701740698` and `46701740698` as the sender as written.
- Whether `sms_header` applies on channels other than SMS; the emulator applies it to SMS only.
- Whether K8 refuses the sender fields and the fields above as it does for an SMS when a request has no SMS recipient,
  such as an e-mail with `html` and no `text`; the emulator does.
- When a held-back send that was itself scheduled reports: at once, or when it would have fallen due; the emulator
  reports it at once, as blocked, and never releases it.
- Whether `sms_character_set`, `sms_format`, `sms_udh`, `sms_originator_source` or `sms_originator_number` are part of
  the key of a duplicate; none was varied alone, and the emulator does not compare them.
- Whether a scheduled send is checked again when it falls due; the emulator checks a send only when it is received.
- Whether two sends exactly five minutes apart count as duplicates; the emulator holds the later one back, so its window
  includes the five minutes.
