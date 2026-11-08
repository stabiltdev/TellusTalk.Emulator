---
status: accepted
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-serve-tellustalks-paths-at-the-root: Serve TellusTalk's paths at the root, as TellusTalk serves them

## Context and Problem Statement

An app runs its real TellusTalk client against the emulator with only the base address changed. K8 serves its send API
at the root of its host, `https://k8api.tellustalk.com/send/v1`, so a client's base address has never needed a path, and
clients build the request URL in ways that only work without one: a relative `send/v1` against a base address without a
trailing slash replaces its last segment, and an absolute `/send/v1` drops the base address's path altogether.
TellusTalk tells its platforms apart by host; an emulator serving more than one would have to tell them apart another
way.

## Considered Options

- Serve each platform's API under a prefix of its own, such as `/k8/send/v1`.
- Serve K8's API at the root, `/send/v1`, as TellusTalk does.

## Decision Outcome

**TellusTalk's API is served at the root, at exactly the path TellusTalk serves it: K8's send API is `/send/v1`.** The
emulator's own routes, the read API under `/api/v1/` and the page at `/`, are the same whatever platform a message came
through, so they carry no platform in their path.

A prefix works only for a client that builds its URL in one particular way, and for any other it fails with a `404` that
points nowhere. At the root every client works.

## Consequences

- **Good:** a client works against the emulator with its base address alone changed, however it builds the URL.
- **Bad:** the root belongs to one platform, so another would need another way in.
- **Follow-up:** if the emulator serves another TellusTalk platform, that platform gets a prefix of its own, such as
  `/legacy/send/v1`, and K8 gets `/k8/send/v1` beside the root at the same time; the root changes to another platform
  only in a new major version.
