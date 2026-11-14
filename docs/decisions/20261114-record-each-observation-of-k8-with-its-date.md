---
status: accepted
date: 2026-11-14
deciders: [Peter Hedberg]
supersedes: [ADR 20261108-follow-observed-behaviour-before-the-specification](20261108-follow-observed-behaviour-before-the-specification.md)
---

# ADR 20261114-record-each-observation-of-k8-with-its-date: Follow TellusTalk's observed behaviour before its specification, and record each observation with its date

## Context and Problem Statement

Probing on a TellusTalk account of the project's own makes observations frequent, and the README held only what was not
observed. A reader could not tell what was proven, or since when, and a later change in TellusTalk's API could not be
told from an old observation.

## Considered Options

- Keep the list of unobserved behaviour in the README.
- Keep a page that lists each observation with its date, and everything not observed.

## Decision Outcome

**The rule of [ADR 20261108-follow-observed-behaviour-before-the-specification](20261108-follow-observed-behaviour-before-the-specification.md)
stands: where K8 was observed the emulator does what it did, elsewhere it follows the specification.
`docs/k8-behaviour.md` lists each observation with its date and everything not observed, and an item moves from one
part to the other in the change that settles it.**

The page replaces the README's list, so what is proven and what is a reading of the specification sit side by side.

## Consequences

- **Good:** a reader sees what is proven and since when.
- **Bad:** the page must be kept current.
- **Follow-up:** re-probe an old observation when TellusTalk changes its API.
