---
status: accepted
date: 2026-11-15
deciders: [Peter Hedberg]
---

# ADR 20261115-serve-the-emulators-own-api-without-a-version: Serve the emulator's own API without a version in its paths

## Context and Problem Statement

The emulator's own API, the messages, events, send failures and health, was served under `/api/v1/`. Version 1.0.0
fixes the read API's shape, so its paths have to be settled before then. Both packages run the image of their own
version, as [ADR 20261111-ship-an-aspire-package-on-the-oldest-aspire-without-known-vulnerabilities](20261111-ship-an-aspire-package-on-the-oldest-aspire-without-known-vulnerabilities.md)
and [ADR 20261111-ship-a-testcontainers-package-of-the-images-version](20261111-ship-a-testcontainers-package-of-the-images-version.md)
decide, so a test always meets the API of the image it runs.

## Considered Options

- Keep `/api/v1/`, and serve a `/api/v2/` beside it when the API breaks.
- Serve the API under `/api/`, and let the image's version be the API's.

## Decision Outcome

**The emulator's own API is served under `/api/`, without a version; the image's version is the API's.** The messages
are at `/api/messages`, the events at `/api/events`, the send failures at `/api/send-failures` and the health check at
`/api/health`. TellusTalk's `send/v1` keeps its version, since it is TellusTalk's path, served as
[ADR 20261108-serve-tellustalks-paths-at-the-root](20261108-serve-tellustalks-paths-at-the-root.md) decides.

A version in the path pays when one server answers clients of several contracts at once. The emulator answers one test
run from one image, and a test that wants the old contract pins the old image, so a `/api/v2/` beside `/api/v1/` would
keep code alive for a client that has a simpler way out. Without it, `v1` only repeats what the image's tag already
says.

## Consequences

- **Good:** the paths name what they serve and nothing more, and a breaking change to the API is a new major version of
  the image and of both packages, where a test already looks for one.
- **Bad:** a client written against one image meets a `404` on an image whose API has moved, and a package given an
  image of another version than its own meets a health check at a path that image may not serve.
- **Follow-up:** none.
