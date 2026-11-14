# TellusTalk Emulator

A local stand-in for [TellusTalk](https://tellustalk.com/)'s
[K8 send API](https://k8api.tellustalk.com/openapi_docs/redoc.html). It catches the SMS an app sends and shows them on a
web page, so nothing reaches a real phone during development and testing.

Not affiliated with TellusTalk.

## What it does

- Takes `POST /send/v1` the way TellusTalk's K8 platform does, for SMS, so an app runs its real TellusTalk client
  against it with only the base address changed.
- Answers with a job id in TellusTalk's form and the headers TellusTalk sends.
- Keeps every SMS in memory and shows it on a page at `/`: a list of the sent and the scheduled, and each SMS as the
  recipient's phone shows it, with the parts it splits into.
- Serves what it caught to automated tests through a read API under `/api/v1/`.
- Posts a delivery report, in the form TellusTalk posts one, to each URL in a message's `report_to`: delivered, or
  failed for a number set to fail, and on request from the page or the read API.
- Refuses only what TellusTalk refuses and what it cannot record; everything TellusTalk's specification forbids but
  TellusTalk accepts becomes a warning on the message.
- Answers the next sends, to any number or to one, with the `4xx` and `5xx` a test arms, late if asked, so a client's
  retries and timeouts can be tried.

It sends no SMS onward. Its only outgoing calls are the delivery reports to the URLs a request names.

## What it covers

| | Covered | Not yet |
|---|---|---|
| Platform | K8, `k8api.tellustalk.com` | The legacy platform, `api.tellustalk.com`; the HTTP-GET API; e-mail-to-SMS |
| API | `POST /send/v1` | K8's other APIs |
| Channel | SMS, with every field the specification defines for it | MMS, e-mail, fax, voice and groups, which beside an SMS recipient are counted but not caught, and alone are refused; `log:` addresses, secure messages, signatures and payments, which are refused |
| Body | JSON | XML and form bodies |
| Delivery reports | JSON to http(s) URLs, automatically and on request | Reports by e-mail or SMS, other report formats, retries |
| Replies | `reply_to` is kept and shown | Replies themselves |
| Storage | In memory | Persistence: a restart empties the lists |
| Failures | TellusTalk's own `401` and `400` sender refusals, refusals of what the emulator cannot record, and a list of `4xx` and `5xx`, late if asked, for the next sends to any number or to one, on request | Dropped connections |

## Running it

```shell
docker run --rm -p 8080:8080 ghcr.io/stabiltdev/tellustalk-emulator:<version>
```

A release is tagged with its version, such as `0.1.0`, with its major and minor version, `0.1`, and with `latest`; a
pre-release, such as `0.2.0-beta.1`, only with its version.

Open `http://localhost:8080/` for the page, and give the app `http://localhost:8080/` where it would have
`https://k8api.tellustalk.com/`. Any user name and password are accepted, but the credentials must be sent up front:
TellusTalk answers `401` without a `WWW-Authenticate` challenge, and so does the emulator.

### From an Aspire AppHost

The package [`Stabilt.TellusTalk.Emulator.Aspire`](src/Stabilt.TellusTalk.Emulator.Aspire/README.md) adds the emulator,
healthy once it answers, and gives an app its address:

```csharp
var tellusTalk = builder
    .AddTellusTalkEmulator("tellustalk")
    .WithSmsSender(TellusTalkSmsSender.Text("Acme"));

builder
    .AddProject<Projects.Api>("api")
    .WithReference(tellusTalk)
    .WaitFor(tellusTalk);
```

`WithReference` sets `TellusTalk__BaseUrl`, the setting `TellusTalk:BaseUrl`; its second argument names another. The
credentials still come from the app's own configuration, and any will do. The package runs the image of its own
version. Without the package, the same container is:

```csharp
var tellusTalk = builder
    .AddContainer("tellustalk", "ghcr.io/stabiltdev/tellustalk-emulator", "<version>")
    .WithHttpEndpoint(targetPort: 8080, name: "http")
    .WithHttpHealthCheck("/api/v1/health", endpointName: "http");
```

### From Testcontainers

The package [`Stabilt.TellusTalk.Emulator.Testcontainers`](src/Stabilt.TellusTalk.Emulator.Testcontainers/README.md)
starts the emulator once it answers, and gives its address:

```csharp
await using var emulator = new TellusTalkEmulatorBuilder()
    .WithSmsSender(TellusTalkSmsSender.Text("Acme"))
    .Build();

await emulator.StartAsync();

var baseAddress = new Uri(emulator.GetBaseAddress());
```

It has the Aspire package's settings, by the same names, and runs the image of its own version. Without the package, the
same container is:

```csharp
var emulator = new ContainerBuilder("ghcr.io/stabiltdev/tellustalk-emulator:<version>")
    .WithPortBinding(8080, assignRandomHostPort: true)
    .WithWaitStrategy(Wait
        .ForUnixContainer()
        .UntilHttpRequestIsSucceeded(request => request
            .ForPort(8080)
            .ForPath("/api/v1/health")))
    .Build();

await emulator.StartAsync();

var baseAddress = new Uri($"http://{emulator.Hostname}:{emulator.GetMappedPublicPort(8080)}/");
```

### Reading what was sent in a test

```csharp
using var emulatorApi = new HttpClient { BaseAddress = baseAddress };

// Before each test: start from empty lists.
await emulatorApi.DeleteAsync("api/v1/messages");

// … the code under test sends an SMS …

var list = await emulatorApi.GetFromJsonAsync<JsonElement>("api/v1/messages?to=%2B46701740605");
var message = list.GetProperty("messages")[0];

Assert.Equal("Your code is 482913", message.GetProperty("text").GetString());
Assert.Equal("Acme", message.GetProperty("sender").GetProperty("shown").GetString());
Assert.Empty(message.GetProperty("warnings").EnumerateArray());
```

### Settings

| Environment variable | Default | Meaning |
|---|---|---|
| `Emulator__SmsSender` | `replysms` | The user's Sender in TellusTalk: `replysms`, `text:<name>`, `number:<number>` or `subject`. Shown when a request has no sender field. |
| `Emulator__ReplyNumber` | `+46701740698` | The number TellusTalk assigns for replies, shown for `replysms`, for Sender `replysms` or `subject`, and when the sender field K8 chose is empty. |
| `Emulator__AccountSender` | none | Obsolete. Honoured only when `Emulator__SmsSender` is not set: a value of `+` and digits means `replysms` with that number as `ReplyNumber` (unless `ReplyNumber` is set); anything else means `text:<value>`. |
| `Emulator__RetentionLimit` | `1000` | How many messages are kept; the oldest go first. |
| `Emulator__ReportDelaySeconds` | `1` | How long after a message is sent, or falls due, its delivery report goes out. |
| `Emulator__FailingNumbers` | none | Numbers whose reports say the operator could not deliver, separated by commas. |

## Refusals and warnings

The emulator refuses:

- what TellusTalk refuses, each with TellusTalk's own text as `text/html`: a request without credentials, or with
  malformed ones, with `401`; and with `400`, a sender field (`subject`, `sms_originator_source`,
  `sms_originator_text` or `sms_originator_number`) that is null or not a string (`Invalid subject not a string: None`),
  an `sms_originator_source` other than `replysms`, `text`, `number` or `subject`, matched exactly, case included, and a
  source of `subject`, `text` or `number` without its value (`subject`, `sms_originator_text` or
  `sms_originator_number`, absent or empty). They are checked in that order, which TellusTalk has not been seen to
  settle, and before the emulator's own limits;
- what it cannot record, each with a `text/plain` body that begins `TellusTalk Emulator:` and says it is the
  emulator's limit: a body that is not JSON (`415`); and with a `400`, JSON that does not parse or is not an object, a
  request without `to`, a `to` that is neither an address nor a list of addresses, recipient objects (the form secure
  messages and signatures take), a request with recipients but none on `sms:`, and a `log:` recipient, which TellusTalk
  answers with a `log_href` the emulator cannot give.

Everything else is accepted and answered as TellusTalk answers it. What TellusTalk's specification forbids, or what
TellusTalk was seen to correct quietly, becomes a warning on the message, shown on the page and served by the read API:

| Code | When |
|---|---|
| `unknown-field` | A field no send schema defines; TellusTalk ignores it. |
| `wrong-json-type` | A field of another JSON type than the schema's, such as `"sms_flash": "true"`. |
| `to-missing-prefix` | A recipient without `sms:`. |
| `to-spaces-removed` | Spaces in a recipient's number; TellusTalk removes them, and so does the emulator. |
| `to-not-digits` | A recipient that is not `+` followed by digits. |
| `originator-text-too-long` | `sms_originator_text` over 11 characters; TellusTalk passes it on whole. |
| `subject-too-long` | `subject` as the sender, named or chosen by TellusTalk, over 11 characters. |
| `originator-number-invalid` | `sms_originator_number` that is not `+` followed by digits. |
| `single-thread-without-replysms` | `sms_single_thread` when the source that decided the sender, or the user's Sender setting when it decided, is not `replysms`. |
| `character-set-invalid` | An `sms_character_set` that is not `optimize`, `gsm`, `unicode` or `binary`. |
| `format-deprecated` | `sms_format`, which `sms_character_set` replaces. |
| `binary-without-udh` | A binary SMS without `sms_udh`. |
| `binary-not-hex` | A binary SMS whose text is not hex. |
| `gsm-unsupported-characters` | Characters outside the GSM alphabet under `sms_character_set: gsm`. |
| `max-parts-invalid` | `sms_max_parts` below 1. |
| `max-parts-exceeded` | A text needing more parts than `sms_max_parts` allows; the emulator keeps it whole. |
| `billing-id-invalid` | A `billing_id` that is not alphanumeric. |
| `scheduled-time-invalid` | A `scheduled_send_time` that is not an ISO 8601 date and time; the message is sent at once. |
| `report-address-invalid` | A `report_to` or `reply_to` that is no e-mail address, `sms:` number or http(s) URL. |

A test can assert that a message carries no warnings, and so catch what TellusTalk would let through.

## Delivery reports

A message with a URL in `report_to` gets a report `Emulator__ReportDelaySeconds` after it is sent, or after it falls
due if it was scheduled. The report says delivered, or failed when the recipient is one of `Emulator__FailingNumbers`,
so an app's failure path can be tried with a number of its own. A report of either kind can also be sent at once, any
number of times: from the buttons beside the message on the page, or with `POST /api/v1/messages/{id}/reports`.

Each report is a `POST` with the JSON TellusTalk was seen sending:

```json
{
  "log_status": "txtDELIVERED",
  "log_status_code": 22,
  "failed_reason": "",
  "article_id": "SMS_Sent",
  "to_protocol": "SMS",
  "to_address": "sms:+46701740605",
  "to_name": null,
  "from_address": "user_id:tester",
  "from_name": "",
  "subject": null,
  "text": "Your order has shipped.",
  "html": "Your order has shipped.",
  "echo": null,
  "quantity": 1,
  "log_uuid": "5d656a5cdafd11f1ba39a2551b38e481",
  "conversation_uuid": "5d656a5cdafd11f1ba39a2551b38e481",
  "job_uuid": "5d656a5cdafd11f1ba39a2551b38e481",
  "report_time": "2026-11-07T22:00:01.000Z",
  "failed_details": null,
  "text_linebreaksbr": "Your order has shipped.",
  "template_optional_line": "--------------"
}
```

A failed report has `"log_status": "txtDELIVERY_FAILED"`, `"log_status_code": 21` and
`"failed_reason": "txtFAILED_REASON: txtOPERATOR_ROUTING_ERROR, None"`. `echo` carries the request's `echo` as it was
sent, and `quantity` the number of parts.

The request carries `Content-Type: application/json`, `Accept: */*`, a `webpages-request-id` and an
`x-webpages-request-id` with the same value, and every header in the address's settings, such as
`https:{"headers":{"Authorization":"Basic …"}}://example.se/reports`. A `content_type` other than `application/json`
in the settings is recorded as not sent, since TellusTalk's other report formats have not been seen.

Every attempt, with the receiver's status or why it was not sent, is kept on the message: on the page and in the read
API's `reports`. The emulator checks the receiver's certificate, so from inside a container an app on the host is best
given an `http://host.docker.internal:<port>/…` address.

## The read API

| Request | Answer |
|---|---|
| `GET /api/v1/health` | `Healthy`, as `text/plain` |
| `GET /api/v1/messages?state=sent\|scheduled&to=<number>&q=<text>` | `{"total": <n>, "messages": [<message>, …]}`, newest first; every filter is optional, and another `state` is answered `400` |
| `GET /api/v1/messages/{id}` | `<message>`, or `404` |
| `DELETE /api/v1/messages/{id}` | `204`, or `404` |
| `DELETE /api/v1/messages` | `204`; both lists are emptied |
| `POST /api/v1/messages/{id}/reports` with `{"status":"delivered"}` or `"failed"` | The attempts, as in a message's `reports`, once the report has been sent; `404` for an unknown message, `400` for another status |
| `GET /api/v1/events` | Server-sent events of type `change`, with `{"kind":"added"}`, `removed`, `cleared`, `due` or `reported` |
| `PUT /api/v1/send-failures` with `[<failure>, …]` | The failures armed for any send, as `GET` reads them; see [failing sends](#failing-sends) |
| `PUT /api/v1/send-failures/{number}` with `[<failure>, …]` | The same, for sends to that number only |
| `GET /api/v1/send-failures` or `GET /api/v1/send-failures/{number}` | `[{"status":503,"remaining":1,"delaySeconds":0}, …]`, what is left in the order it will be spent; `[]` when nothing is armed |
| `DELETE /api/v1/send-failures/{number}` | `204`; sends to that number no longer fail |
| `DELETE /api/v1/send-failures` | `204`; no send fails, to any number |

A `400` or `415` from the read API carries `{"message": "…"}`, saying what is wrong; that includes a body that is
missing, is not JSON, or is not the JSON the endpoint takes.

A message:

```json
{
  "id": "5d656a5cdafd11f1ba39a2551b38e481",
  "jobId": "5d656a5cdafd11f1ba39a2551b38e481",
  "state": "sent",
  "to": "+46701740605",
  "toAsSent": "sms:+46 70 174 06 05",
  "notCaught": [],
  "user": "tester",
  "receivedAt": "2026-11-07T22:00:00+00:00",
  "dueAt": null,
  "sender": { "shown": "Probe", "kind": "name", "source": "text", "fromAccount": false },
  "text": "Your order has shipped.",
  "links": [],
  "parts": { "characterSet": "gsm", "count": 1, "length": 23, "starts": [0], "leftInLastPart": 137, "maxParts": 8 },
  "reportTo": [ { "kind": "url", "target": "https://example.se/reports", "contentType": null, "headers": {} } ],
  "replyTo": [],
  "fields": { "to": "sms:+46 70 174 06 05", "text": "Your order has shipped.", "sms_originator_source": "text", "sms_originator_text": "Probe", "report_to": "https://example.se/reports" },
  "warnings": [ { "code": "to-spaces-removed", "field": "to", "text": "…" } ],
  "reports": [ { "at": "2026-11-07T22:00:01+00:00", "status": "delivered", "target": "https://example.se/reports", "sent": true, "outcome": "200 OK" } ],
  "request": "<the body as it arrived>"
}
```

- A single recipient's message takes the job's id; each of several recipients gets an id of its own in the same job.
- `notCaught` lists the request's recipients on `email:`, `fax:`, `mms:`, `voice:` and `group:`, as sent. TellusTalk
  sends to them and `job_size` counts them, but the emulator catches SMS only.
- `sender.source` is the source the request named, else the field TellusTalk chose (`subject`, `text` or `number`),
  else `account` when the settings decided; whether the request named it is in `fields`. `fromAccount` says the shown
  sender came from the settings or the reply number rather than from the request.
- `text` is what the phone shows: `sms_header` in front, and each `redirect=` followed by an http(s) URL and each
  `--LINK_VIEW--` replaced by a short link in the form of the specification's examples, `https://ebox.nu/r_xxxxxx/0`
  and `https://ebox.nu/z_xxxxxx`, with a placeholder id. `links` lists them in order, each with its `kind`, `redirect`
  or `view`, the link as `shown`, and the `target` a redirect leads to; a view link's `target` is `null`.
- `parts.length` is in septets for `gsm`, UTF-16 code units for `ucs2` and bytes for `binary`; `starts` are where each
  part begins in `text`.
- A number in `to` may be written with its `+` unescaped; it arrives as a space and is read as `+`.
- `reports` lists every report attempt, oldest first. `sent` says whether the report reached the receiver, whatever it
  answered; `outcome` is the receiver's status, or why the report was not sent or not delivered.

## Failing sends

A test can make sends fail, to try its client's retries, timeouts and the point where it gives up. It arms a list of
failures before the code under test sends; the sends spend them in order, and once they are spent, sends go through
again.

```http
PUT /api/v1/send-failures/+46701740605
Content-Type: application/json

[{"status":503,"count":2},{"status":429,"delaySeconds":5}]
```

The next two sends to `+46701740605` are answered `503`, the third `429` five seconds later, and the fourth goes
through. Sends to other numbers are left alone, so tests that each send to a number of their own can share one
emulator. Without a number, `PUT /api/v1/send-failures` arms failures for any send, as when TellusTalk is down.

- A failure has a `status` from 400 to 599, by default 500; a `count`, by default 1; and `delaySeconds`, how long the
  send waits before it is answered, by default 0. A `PUT` without a body arms one failure with 500. Another status, a
  count below 1, a negative delay, or a number that is not `+` followed by digits is answered `400`.
- A `PUT` replaces what was armed for its number, or for any send.
- A failing send is answered with the status, its reason phrase as `text/plain`, such as `Service Unavailable`, and
  TellusTalk's headers, and nothing is recorded. A `401` is answered as TellusTalk answers a send without credentials.
- Failures for any send are spent before the request is read. A number's are spent once it is read, by the first
  recipient in `to` that has any; a send spends one failure at most, whatever its recipients.
- A delay longer than the client's own timeout tries that timeout, and a client that gives up spends the failure all
  the same.
- Credentials are checked first, so a send without them is answered `401` and spends no failure.
- `DELETE /api/v1/send-failures/{number}` clears a number, and `DELETE /api/v1/send-failures` clears everything, every
  number included. `DELETE /api/v1/messages` leaves failures as they are.

## Behaviour that follows the specification, not observation

Where TellusTalk's behaviour has not been observed, the emulator follows its
[published specification](https://k8api.tellustalk.com/openapi_docs/redoc.html).
[docs/k8-behaviour.md](docs/k8-behaviour.md) lists what was observed, with the dates, and what was not.

## Building

```shell
dotnet build Stabilt.TellusTalk.Emulator.slnx -c Release
dotnet test --solution Stabilt.TellusTalk.Emulator.slnx
dotnet publish src/Stabilt.TellusTalk.Emulator -c Release -r linux-x64 -t:PublishContainer -p:LocalRegistry=Docker
```

The last builds the image into the local Docker as `stabiltdev/tellustalk-emulator:latest`; without `LocalRegistry`,
the SDK would prefer a platform's own container tool, such as `wslc` on Windows, where one runs. Without `-r`, it builds
for both `linux-x64` and `linux-arm64` as one image index, which needs a registry (`-p:ContainerRegistry=…`) or an
archive (`-p:ContainerArchiveOutputPath=…`) rather than the local Docker; a version tag in CI publishes it that way to
`ghcr.io`.

The Testcontainers package's tests start the image named by `TELLUSTALK_EMULATOR_TEST_IMAGE`, such as
`stabiltdev/tellustalk-emulator:latest` once it is built, and are skipped without it; CI builds the image and names it.

Why the emulator works as it does is recorded in [docs/decisions](docs/decisions/README.md), and how its code and tests
are written in [docs/conventions.md](docs/conventions.md). [CONTRIBUTING.md](CONTRIBUTING.md) says how to report a
problem or propose a change.

## Licence

MIT; see [LICENSE](LICENSE). The page's font, Schibsted Grotesk, is under the SIL Open Font License 1.1; see
[`src/Stabilt.TellusTalk.Emulator/wwwroot/fonts/OFL.txt`](src/Stabilt.TellusTalk.Emulator/wwwroot/fonts/OFL.txt).
