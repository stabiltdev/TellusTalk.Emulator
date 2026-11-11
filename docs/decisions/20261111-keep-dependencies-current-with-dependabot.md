---
status: accepted
date: 2026-11-11
deciders: [Peter Hedberg]
---

# ADR 20261111-keep-dependencies-current-with-dependabot: Keep dependencies current with Dependabot, and move Aspire.Hosting only for a vulnerability

## Context and Problem Statement

The NuGet packages, the .NET SDK and the workflow's actions were updated by hand, when someone remembered to. An
advisory against `Aspire.Hosting` came to light only when the package audit failed a build. And the Aspire package has
to stay on the oldest `Aspire.Hosting` without a known vulnerability, which moving every package to its newest breaks.

## Considered Options

- Update by hand, as before.
- Dependabot, with a pull request for each update.
- Dependabot, with each week's minor and patch updates in one pull request.

## Decision Outcome

**Dependabot proposes updates to the NuGet packages, the .NET SDK in `global.json` and the workflow's actions once a
week, and a security update as soon as an advisory is published; `Aspire.Hosting` takes only security updates.**
`.github/dependabot.yml` puts a week's minor and patch updates to the packages in one pull request, and the actions'
updates in another; a major update to a package comes in a pull request of its own. Security updates are on in the
repository's settings. The pull requests' titles open with `build:` or `ci:`, so their merge commits follow Conventional
Commits.

A pull request for each update would bury the few that matter, and a major update is where APIs break, so it is weighed
alone. A security update moves a package to the first version with the fix, which is the version
[ADR 20261111-ship-an-aspire-package-on-the-oldest-aspire-without-known-vulnerabilities](20261111-ship-an-aspire-package-on-the-oldest-aspire-without-known-vulnerabilities.md)
asks for; a version update would move `Aspire.Hosting` to the newest and raise what every AppHost needs. An ignore rule
that names only the package keeps version updates off it and leaves security updates alone.

## Consequences

- **Good:** an advisory against any dependency becomes a pull request without anyone looking for it; dependencies stay
  current at a few pull requests a week; and `Aspire.Hosting` keeps to its own record's rule.
- **Bad:** each update still waits for someone to merge it; one failing update in a grouped pull request holds back the
  rest until it is fixed or split out; and moving `Aspire.Hosting` up for any other reason is done by hand.
- **Follow-up:** none.
