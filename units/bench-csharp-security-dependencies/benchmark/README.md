# Benchmark: third-party dependency risk and its look-alikes

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its authoring
log is [`journal.md`](journal.md).

## Safety note

**This repository deliberately depends on package versions with published security advisories.** They are pinned so
that dependency scanners have something real to find. Nothing here is deployed, published or run against a network:
the API, the worker and the exporter exist to be built, tested and scanned. Do not copy these versions into anything
you run.

## Theme

A small .NET (`net10.0`) **invoice-rendering product** of a fictional software vendor that ships it to customers as an
installable bundle under a proprietary licence:

- `Invoicing.Contracts` — the invoice document model, multi-targeted so the customer's ERP connector can use it;
- `Invoicing.Rendering` — renders an invoice to PDF and to a signed UBL e-invoice;
- `Invoicing.Api` — ASP.NET Core API that renders on request and accepts the legacy ERP webhook;
- `Invoicing.Worker` — background service that takes queued render jobs from the billing MySQL database, renders and
  e-mails them;
- `tools/Invoicing.ArchiveExporter` — a legacy command-line exporter that writes the yearly archive ZIP; it was never
  moved off .NET 6.

Every defect is a fact about a **package version, a licence or a platform**, with a public source (osv.dev /
GitHub advisory, nuget.org deprecation metadata, the package's nuspec licence expression, Microsoft's .NET release
metadata). The source is cited in each key entry's rationale; every advisory id was checked against
`https://api.osv.dev/v1/query` on 2026-10-07.

The rest of the repository has the shared C# scaffolding of `bench-csharp-baseline-clean` (CI pinned by commit SHA,
CodeQL, README, ADRs, architecture doc, CHANGELOG, Central Package Management with committed `packages.lock.json`,
nullable, `src/` + `tests/`), so that the signal is the theme.

## Labels are about truth

A `must-fire` is something a careful reviewer of this product's dependencies would flag. A `must-not-fire` is a site a
careless rule would flag and that reviewer would not. Neither label was chosen to match any scanner. `CPM` below is
`Directory.Packages.props`, where every version is pinned.

## Plants (`must-fire`)

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| DEP-001 | vulnerable | CPM `Newtonsoft.Json` 12.0.3 | GHSA-5crp-9r3c-p9vr / CVE-2024-21907 (high), fixed 13.0.1. Direct, shipped, parses caller-supplied webhook JSON. |
| DEP-002 | vulnerable | CPM `SharpZipLib` 1.3.2 | GHSA-m22m-h4rf-pwq3, GHSA-2x7h-96h5-rq84, GHSA-mm6g-mmq6-53ff (path traversal), fixed 1.3.3. Direct, in the exporter. |
| DEP-003 | vulnerable (transitive) | `src/Invoicing.Rendering/packages.lock.json` `SixLabors.ImageSharp` 1.0.4 | Never declared: the current `PdfSharpCore` 1.3.67 asks for `>= 1.0.4` and NuGet resolves the floor. Seven advisories (e.g. GHSA-65x7-c272-7g7r, GHSA-63p8-c4ww-9cg7, GHSA-2cmq-823j-5qj8). |
| DEP-004 | vulnerable (test-only) | CPM `SharpCompress` 0.28.3 | GHSA-jp7f-grcv-6mjf, GHSA-6c8g-7p36-r338. Referenced only by the unit tests. *Contested*, see below. |
| DEP-005 | deprecated | CPM `Polly.Extensions.Http` 3.0.0 | nuget.org deprecation: *Legacy*, alternate `Microsoft.Extensions.Http.Resilience`. No advisory. |
| DEP-006 | outdated | CPM `CsvHelper` 12.1.2 | Published 2019-01; current 33.1.0 — 21 majors behind. No advisory, not deprecated: staleness only. |
| DEP-007 | licence | CPM `MySql.Data` 26.7.0 | `GPL-2.0-only WITH Universal-FOSS-exception-1.0` in a proprietary shipped component; the licence policy forbids it. |
| DEP-008 | end-of-life platform | `tools/Invoicing.ArchiveExporter/*.csproj` `net6.0` | .NET 6 support ended 2024-11-12; the SDK's EOL warning is switched off on the next line. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | vulnerable | CPM `System.Security.Cryptography.Xml` 10.0.12 | Old versions had CVE-2022-34716 and the 10.0 line had five 2026 advisories (fixed 10.0.10); 10.0.12 is patched. |
| TRP-002 | vulnerable | CPM `MailKit` 4.18.1 (→ `MimeKit` 4.18.1) | MailKit < 4.16.0 and MimeKit < 4.15.1 have 2026 advisories; the pinned versions are past both fixes. |
| TRP-003 | licence | CPM `iTextSharp.LGPLv2.Core` 3.8.6 | A scary name (iTextSharp is known as AGPL) and an id a substring rule mistakes for GPL; it is `LGPL-2.0-only`, which the policy allows for an unmodified library loaded as a separate assembly. |
| TRP-004 | outdated | CPM `FluentAssertions` 7.2.2 | 8.x exists but is commercially licensed; staying on the maintained Apache-2.0 7 line is a recorded decision (ADR 0004), and 7.2.2 is that line's newest release. |
| TRP-005 | malicious | CPM `Bogus` 35.6.5 | A name that reads like a warning; the well-known MIT fake-data generator, in no malicious-package feed. |
| TRP-006 | end-of-life platform | `src/Invoicing.Contracts/*.csproj` `netstandard2.0;net10.0` | .NET Standard is a specification with no end of support, and the other target is net10.0. |
| TRP-007 | pre-release | CPM `Cronos` 0.13.0 | A 0.x number that looks unfinished; it is a stable release (no SemVer pre-release label), the newest on nuget.org. |
| TRP-008 | suppressed diagnostic | `.editorconfig` CA2007 `none` | Off-theme, promoted from scan 1: the reason is on the line above and production code re-enables the rule. |

## Line tolerance is 0

Dependency sites are single lines in `Directory.Packages.props`, packed one per line; with the default tolerance of 3 a
result on one package would also "cover" its neighbours. The key therefore sets `lineTolerance` to 0: a located
result must name the exact `PackageVersion` line (or, for DEP-003, a line of the lock-file entry). A scanner that
reports dependency findings without a location is matched by the harness only through repository-level entries,
which this key deliberately does not use (see the journal: the per-package facts are judged from the raw results).

## Contested truths, and how they were decided

- **DEP-004 — a vulnerable package used only by tests is a must-fire.** The concept is "a declared or locked package
  version affected by a published advisory"; it carries no scope. The package is declared, locked, restored and
  executed on every CI run, on runners that hold repository tokens, and the fix is a version bump. It is lower risk
  than a shipped dependency (fixed fixtures, nothing distributed) — that changes its priority, not whether it is true.
  A scanner that excludes test projects misses it, and that is a real miss.
- **DEP-007 / TRP-003 — licences are judged against this product's written policy.** The repository itself is
  MIT-licensed because it is a public benchmark; the *product it models* is shipped to customers under a proprietary
  licence, and `docs/adr/0003-third-party-licence-policy.md` is its policy: no GPL/AGPL-family licence in a shipped
  component; LGPL allowed for unmodified libraries consumed as separate assemblies; test-only and build-only packages
  are not shipped and not restricted. Under that policy the GPL connector is a violation and the LGPL fork is not.
- **The vulnerable plants are also outdated.** Newtonsoft.Json 12.0.3, SharpZipLib 1.3.2 and SharpCompress 0.28.3 have
  newer releases. A scanner that reports them as outdated as well as vulnerable is right about the fact; one upgrade
  clears both, so such a row is judged *redundant* with the vulnerability plant, not noise and not a separate plant.
  `CsvHelper` (DEP-006) is the one outdated-only plant.
- **One site per transitive plant.** ImageSharp 1.0.4 appears in the lock file of every project that reaches the
  renderer. The key places it once, in the lock file of the project that declares `PdfSharpCore`; the same package in
  another lock file is the same defect (redundant), and lock files are certified clean only for concepts no plant or
  trap can surface in them.
- **Malicious dependency has no plant.** A known-malicious NuGet package is removed from nuget.org, so a repository
  that declared one could not be restored or built (and would distribute malware if it could). The concept is
  covered by a trap (TRP-005) and clean regions only — a deliberate deviation from the coverage matrix, which lists
  must-fire for this repository.

## Dependabot: deliberately absent

The shared scaffolding has a `.github/dependabot.yml`. This repository does not, on purpose: version and security
updates would open pull requests that "fix" exactly the pinned versions the key is about, and an `ignore` list for the
planted packages would itself be a finding a reviewer should raise (silencing security updates). The repository must
stay frozen as written; the omission is part of the benchmark's construction, not of the modelled product, so no key
entry depends on it.

## What is clean

Every tracked file that is not a plant or trap site is certified clean for the concepts this repository covers
(`CLN-*`): vulnerable, outdated, deprecated, pre-release and malicious dependency, licence-policy violation, end-of-life platform,
unpinned CI action and mutable image reference. Lock files are certified only for the concepts no plant or trap can
surface in them (a lock file legitimately records the transitive and test-only plants of the projects it locks).

## Score bands

- `dependencies-not-locked` **[80, 100]** — Central Package Management plus a committed lock file for every project,
  locked-mode restore in CI.
- `build-provenance-and-signing` **[30, 100]** — actions pinned by SHA; nothing is released, so nothing to sign.

## What this repository does not cover

Secrets, injection, IaC and code-health concepts. npm (the TypeScript twin, `bench-ts-security-dependencies`, covers
it). Unused/undeclared dependencies (R8 is a frontend dimension). Findings of concepts the key does not cover are
reported by the harness as *uncovered*, not as noise; an accidental real defect of another kind is fixed in the
repository during authoring (see the journal).

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-csharp-security-dependencies
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-csharp-security-dependencies && dotnet build -c Release && dotnet test -c Release
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-csharp-security-dependencies/benchmark/answer-key.json --taxonomy taxonomy.json
```
