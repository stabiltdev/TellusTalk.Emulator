---
status: accepted
date: 2026-11-08
deciders: [Peter Hedberg]
---

# ADR 20261108-render-the-page-on-the-server-and-swap-it-in-place: Render the page on the server and swap its parts in place

## Context and Problem Statement

The page lists the caught SMS and shows one at a time, live. It is built from Razor components rendered on the server,
without Blazor's runtime, and a small script. Clicking between messages felt slow: the server answered in under a
millisecond, but every click loaded the whole page again, and the event streams of pages left behind held the six
connections a browser opens to a host over HTTP/1.1. A rewrite in React against the read API was considered.

## Considered Options

- A React app against the read API, with the .NET backend kept.
- Server rendering with a full page load on every click.
- Server rendering, with the script fetching the page a click leads to and swapping in what changed.

## Decision Outcome

**The page is rendered on the server, and its script moves between messages, lists and searches by fetching the page it
would load and swapping in the list and the detail, with the address and the browser's history kept in step; without the
script it works as plain linked pages.** The event stream stays open while the page is in use and closes on `pagehide`,
and static files are served with `Cache-Control: no-cache`.

The slowness came from full page loads, not from rendering, and swapping in place removes them. React would bring Node
and a second toolchain into the build, the container image and CI, and a hurdle for anyone contributing to a small .NET
tool.

## Consequences

- **Good:** one toolchain; a click shows the next message without a reload, and the list keeps its scroll position.
- **Bad:** the page's behaviour is split between Razor components and a hand-written script; every part the script
  swaps must carry a stable id.
- **Follow-up:** if the page grows past a list and a detail, the question of a client-side framework comes back.
