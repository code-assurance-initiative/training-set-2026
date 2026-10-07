# Benchmark: npm dependency risk and its look-alikes

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its authoring
log is [`journal.md`](journal.md). It is the TypeScript twin of `bench-csharp-security-dependencies`.

## Safety note

**This repository deliberately depends on package versions with published security advisories.** They are pinned so
that dependency scanners have something real to find. Nothing here is deployed, published or run against a network:
the service and the tool exist to be installed with `npm ci`, tested offline and scanned. Do not copy these versions
into anything you run.

## Theme

A small Node.js 22 + TypeScript **depot dispatch service** of a fictional regional courier, installed on each depot's
own server and licensed to the depot operators under a proprietary licence:

- the service (repository root) — an Express 5 API that builds dispatch runs (city matching, geocoding, delivery
  windows, cut-off times), quotes and fetches labels from a linehaul carrier, rasterises label PDFs for thermal
  printers, bundles a run's labels, and accepts the carrier's signed status feed;
- `tools/manifest-export` — a small legacy command-line tool, its own npm package with its own lock file, that turns
  an exported dispatch run into the CSV manifest the depot printers read. It was never moved off Node.js 16.

Every defect is a fact about a **package version, a licence, a runtime or a declaration**, with a public source
(osv.dev / GitHub advisory, registry.npmjs.org deprecation and licence metadata, the Node.js release schedule) or a
fact of the source tree (what imports what). The source is cited in each key entry's rationale; every advisory id was
checked against `https://api.osv.dev/v1/query` on 2026-10-07.

The rest of the repository has the shared TypeScript scaffolding of `bench-ts-baseline-clean` (CI pinned by commit SHA,
CodeQL, README, ADRs, architecture doc, CHANGELOG, committed lock files installed with `npm ci`, strict TypeScript,
ESLint, Prettier, vitest), so that the signal is the theme. Versions are pinned exactly in `package.json`.

## Labels are about truth

A `must-fire` is something a careful reviewer of this product's dependencies would flag. A `must-not-fire` is a site a
careless rule would flag and that reviewer would not. Neither label was chosen to match any scanner. Every dependency
entry names its package (or runtime) in `subject`, so a scanner that reports per package without a line can be matched.
`root` below is the service's `package.json`; `tool` is `tools/manifest-export/package.json`.

## Plants (`must-fire`)

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| VUL-001 | vulnerable | root `jsonwebtoken` 8.5.1 | GHSA-8cf7-32gw-wr33, GHSA-hjrf-2m68-5959, GHSA-qwph-4952-7xr6, fixed 9.0.0. Direct, shipped, verifies every terminal token. |
| VUL-002 | vulnerable | root `axios` 0.21.1 | 24 advisories (e.g. GHSA-cph5-m8f7-6c5x, GHSA-wf5p-g6vw-rhxx, GHSA-jr5f-v2jv-69x6). Direct, shipped, the carrier client. |
| VUL-003 | vulnerable | tool `minimist` 1.2.5 | GHSA-xvch-5gv4-984h (critical prototype pollution), fixed 1.2.6. The tool's argument parser. |
| VUL-004 | vulnerable (transitive) | root lock `decode-uri-component` 0.2.2 | Never declared: the current geocoding client asks for `query-string <8.x`, whose newest 7.x asks for `^0.2.2`. GHSA-vcc3-ghjq-m6fr, fixed 0.5.0; only an `overrides` entry (absent) can move it. |
| VUL-005 | vulnerable (test-only) | root devDependency `tmp` 0.2.3 | GHSA-52f5-9888-hmc6, GHSA-ph9p-34f9-6g65, fixed 0.2.6. Used only by tests. *Contested*, see below. |
| DPR-001 | deprecated | root `string-similarity` 4.0.4 | registry deprecation on every version ("Package no longer supported"). No advisory. |
| DPR-002 | deprecated (transitive) | root lock `crypto-js` 4.2.0 | Shipped through the geocoding client's URL signer; deprecated ("no longer maintained"). No advisory. |
| OUT-001 | outdated | root `date-fns` 1.30.1 | 2018-12; current 4.4.0 — three majors, seven years. No advisory, not deprecated: staleness only. |
| LIC-001 | licence | root `mupdf` 1.28.1 | `AGPL-3.0-or-later` in a proprietary product installed at customer sites; the licence policy forbids it. |
| EOL-001 | end-of-life runtime | tool `engines.node` `16.x` | Node.js 16 reached end of life 2023-09-11; the tool supports nothing newer. |
| UNU-001 | unused | root `uuid` 14.0.2 | Declared under `dependencies`, imported nowhere (ids come from `crypto.randomUUID`). |
| UND-001 | undeclared | `src/scheduling/cutoff.ts` import of `ms` | Imported at run time, declared nowhere; resolves only because other packages install it. `@types/ms` is types only. |
| MIS-001 | misplaced dev dependency | root `nock` 15.0.1 | An HTTP-mocking library under `dependencies`, imported only by tests. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | vulnerable | root `lodash` 4.18.1 | Older versions carry five advisories; 4.18.1 is past every fix. |
| TRP-002 | vulnerable | root `overrides` `jws` 3.2.3 | `jsonwebtoken` 8.5.1 asks for `^3.2.2` and 3.2.2 is affected (GHSA-869p-cjfg-cm3x); the override and the lock resolve the fixed 3.2.3. |
| TRP-003 | licence | root `tweetnacl` 1.0.3 | `Unlicense` reads like "unlicensed"; it is a permissive public-domain dedication, allowed by the policy. |
| TRP-004 | licence | root `jszip` 3.10.2 | `(MIT OR GPL-3.0-or-later)` is a choice; the policy elects MIT. A substring rule sees `GPL-3.0`. |
| TRP-005 | malicious | root devDependency `@tsconfig/node22` | An internal-looking scope; the public TypeScript base-config package, in no malicious-package feed. |
| TRP-006 | unused | root devDependency `@tsconfig/node22` | Imported nowhere because `tsconfig.json` `extends` it. |
| TRP-007 | pre-release | root `xml2js` 0.6.2 | A 0.x number; a stable release, the newest on the registry. |
| TRP-008 | outdated | root devDependency `@types/node` 22.x | Newer majors exist; the types follow the Node.js 22 runtime by decision (ADR 0003), newest of the 22 line. |
| TRP-009 | end-of-life runtime | root `.nvmrc` `22` | Node.js 22 left *active* LTS in 2025-10 but is supported (maintenance LTS) until 2027-04-30. |
| TRP-010 | undeclared | `src/lifecycle.ts` import of `timers/promises` | A Node.js built-in imported without the `node:` prefix, not a package. |
| TRP-011 | deprecated | root `uuid` 14.0.2 | uuid 10 and below are deprecated on the registry; 14.0.2 is not. |
| TRP-012 | outdated | root devDependency `typescript` 6.0.3 | TypeScript 7 exists; the linter supports `<6.1`, so the compiler moves with it (ADR 0004); newest 6.0 release. |
| TRP-013 | nullable analysis off | repository (`tsconfig*.json`) | Promoted from scan 1: `strict` is inherited from `@tsconfig/node22` through `extends`, for both configurations. |
| TRP-014 | authorization enforcement | repository (`src/http`) | Promoted from scan 1: the scopes are named in one module and declared per route where it is registered. |

## Line tolerance is 0

Dependency sites are single lines of `package.json`, packed one per line; with the default tolerance of 3 a result on
one package would also "cover" its neighbours. The key sets `lineTolerance` to 0: a located result must name the exact
line (for the two lock-file plants, a line of the package's lock entry). A scanner that reports a package without a
location — or locates it in a manifest or lock file — is matched through the entry's `subject` (contract 1.2).

## Contested truths, and how they were decided

- **VUL-005 — a vulnerable package used only by tests is a must-fire**, as in the C# twin (its DEP-004). The concept is
  "a declared or locked package version affected by a published advisory"; it carries no scope and no
  exploitability condition. The package is declared, locked, installed by `npm ci` and executed on every CI run, on
  runners that hold repository tokens, and the fix is a version bump. That neither advisory is reachable through the
  tests' fixed options lowers its priority, not its truth. A scanner that skips devDependencies misses it, and that is
  a real miss.
- **VUL-004 / DPR-002 — transitive facts sit in the lock file.** Both packages arrive through the current release of
  the geocoding client; neither is declared. The key places each once, at its entry in the root `package-lock.json`;
  a result that names the parent instead (e.g. "`query-string` depends on a vulnerable version") restates the same
  defect and is judged redundant, not a separate plant.
- **LIC-001 / TRP-003 / TRP-004 — licences are judged against this product's written policy.** The repository itself
  is MIT-licensed because it is a public benchmark; the *product it models* is installed on customer premises under a
  proprietary licence, and `docs/adr/0002-third-party-licence-policy.md` is its policy: no GPL/AGPL-family licence in
  shipped code; a disjunctive licence expression is used under its permissive option; Unlicense, 0BSD, BlueOak and
  similar permissive licences are allowed; devDependencies are not shipped and not restricted.
- **The vulnerable plants are also outdated.** jsonwebtoken 8.5.1, axios 0.21.1, minimist 1.2.5 and tmp 0.2.3 have
  newer releases. A scanner that reports them as outdated as well is right about the fact; one upgrade clears both,
  so such a row is judged *redundant* with the vulnerability plant, not noise and not a separate plant. The same holds
  for `@types/jsonwebtoken` 8.x, which follows jsonwebtoken's major. `date-fns` (OUT-001) is the one outdated-only
  plant.
- **The test-only package is planted where it is declared.** MIS-001 sits on the `dependencies` line that a reviewer
  would move; a scanner that points at the test file importing it has found the evidence, not the site, and the
  journal judges such a result by hand.
- **Malicious dependency has no plant.** A known-malicious npm package is removed from the registry, so a repository
  that declared one could not be installed with `npm ci` (and would distribute malware if it could). The concept is
  covered by a trap (TRP-005) and clean regions only — a deliberate deviation from the coverage matrix, as in the C#
  twin.
- **No install-script plant.** The coverage matrix assigns no install-script concept to this repository. The only
  package in either tree that declares an install script is `fsevents`, an optional, macOS-only development package
  of the test runner, which is not installed on Linux.

## Dependabot: deliberately absent

The shared scaffolding has a `.github/dependabot.yml`. This repository does not, on purpose (as the C# twin): version
and security updates would open pull requests that "fix" exactly the pinned versions the key is about, and an `ignore`
list for the planted packages would itself be a finding a reviewer should raise (silencing security updates). The
repository must stay frozen as written; the omission is part of the benchmark's construction, not of the modelled
product, so no key entry depends on it. For the same reason CI *reports* `npm audit` and `npm outdated` into the job
summary instead of failing on them.

## What is clean

Every tracked file outside `benchmark/` that is not a manifest is certified clean (`CLN-*`) for the concepts this
repository covers: vulnerable, outdated, deprecated, pre-release and malicious dependency, licence-policy violation,
end-of-life platform, unused, undeclared and misplaced dependency, unpinned CI action and mutable image reference — a
file that holds a plant or trap site is certified for the other concepts. The two lock files are certified only for the
concepts no plant or trap can surface in them (a lock file legitimately records every package of its tree). The two
`package.json` files have no clean entry: every dependency line is a plant, a trap or a dependency nobody labelled.

## Score bands

- `dependencies-not-locked` **[80, 100]** — both packages commit a lock file; CI installs with `npm ci`.
- `build-provenance-and-signing` **[30, 100]** — actions pinned by SHA; nothing is released, so nothing to sign.
- `outdated-dependency` **[30, 85]** — three of seventeen shipped direct dependencies a major or more behind.

## What this repository does not cover

Secrets, injection, IaC and code-health concepts (sibling repositories cover them). NuGet (the C# twin covers it).
Findings of concepts the key does not cover are reported by the harness as *uncovered*, not as noise; an accidental
real defect of another kind is fixed in the repository during authoring (see the journal).

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-ts-security-dependencies
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-ts-security-dependencies && npm ci && npm test
(cd tools/manifest-export && npm ci && npm test)
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-ts-security-dependencies/benchmark/answer-key.json --taxonomy taxonomy.json
```
