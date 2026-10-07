# training-set-2026

The **2026 training set** of the [scanner-benchmark](https://github.com/code-assurance-initiative/scanner-benchmark):
25 small, realistic code repositories ("units"), C# and TypeScript. Each unit contains **exactly** the defects its
answer key says it contains, plus **traps** that look like defects and are not. Every unit is labelled against
rubric generation **`rubric-2026.10.1`**.

This repository holds the units. The harness that scores a scanner against them (answer-key schema, scanner-neutral
taxonomy, scanner mappings, scoring CLI, recorded results) is
[scanner-benchmark](https://github.com/code-assurance-initiative/scanner-benchmark).

> **Planted secrets.** The security units contain deliberately hard-coded secrets in their code and in their git
> history. They were generated for this benchmark, have the real format, and authenticate nothing. Do not report them.

## Layout

```
registry.json             every unit: language, family, phase, latest tag, commit, keySha256, bundle sha256,
                          snapshot path, and every registered version (superseded versions stay valid)
units/<unit>.bundle       the unit as a git bundle: full history, every branch and every tag
units/<unit>/             readable snapshot: the tree of the unit's LATEST tag (no .git), for browsing
tools/materialize.sh      bundle -> freestanding clone, checked out at the latest (or a chosen) tag
tools/verify.sh           bundle sha256, tags -> commits, key hashes, snapshot == tag tree
tools/build_set.py        how this set was built from the authoring clones (verifies before it writes)
```

**Scan the materialised clone, never the snapshot.** The snapshot has no history, and several units measure history:
secrets that exist only in old commits, scripted sprint histories (hotspots, knowledge silos, change coupling, a
regression introduced and fixed), tags and changelogs. The snapshot is there to read the code and the answer key
(`units/<unit>/benchmark/answer-key.json`, `units/<unit>/benchmark/README.md`) without cloning anything.

## Materialise a unit

```sh
git clone https://github.com/code-assurance-initiative/training-set-2026
cd training-set-2026
tools/verify.sh                                         # every bundle, tag, commit and key hash vs registry.json
tools/materialize.sh bench-csharp-security-secrets      # -> ../training-set-2026-units/bench-csharp-security-secrets
tools/materialize.sh --all [dest]                       # every unit, into <dest>/<unit>
tools/materialize.sh bench-csharp-domain-events [dest] --tag v1.0.0     # a superseded version
```

Each unit lands in `<dest>/<unit>` — a directory named exactly after the unit, because scanners name their output
after the directory they scan and because the harness reads results relative to it. The clone has the full history and
all tags, no remote, and is checked out at the unit's latest registered tag (on `main` when `main` is at that tag, as
in the authoring clone). `materialize.sh` refuses a destination inside a git work tree: a unit nested inside another
repository would be scanned, and its history read, as part of that repository. Default destination:
`../training-set-2026-units`, next to this checkout.

## Score a scanner

1. Materialise the units (above).
2. Scan each `<dest>/<unit>` with the scanner in its **default configuration**, SARIF 2.1.0 out.
3. Score with the harness:

   ```sh
   git clone https://github.com/code-assurance-initiative/scanner-benchmark && cd scanner-benchmark
   python3 -m cai_bench score --key <dest>/<unit>/benchmark/answer-key.json --sarif report.sarif \
       --mapping mappings/<scanner>.json [--scores scores.json] [--json report.json]
   ```

   A scanner needs a mapping file (`mappings/<scanner>.json`, concept -> rule ids) before it can be scored; the
   harness README explains labels, outcomes, matching rules and secondary configurations.

To re-measure Watchdog against its frozen baseline (all 25 units, `rescore.py` + `compare`), follow
[BASELINE-2026-10-07 § 11](https://github.com/code-assurance-initiative/scanner-benchmark/blob/main/results/watchdog/BASELINE-2026-10-07.md#11-re-measuring-against-this-baseline):
materialise -> scan contained -> `results/watchdog/rescore.py --units-dir <dest> --scans-root …` ->
`python3 -m cai_bench compare`. The harness reads every answer key from the materialised units (`--units-dir`) or
straight from this repository's bundles (`--set-dir`), and checks each against the `keySha256` registered here.

## Versioning

- **Unit versions** are git tags *inside* each bundle (`v1.0.0`, `v1.1.0`, …). A frozen tag and its registered key
  hash never change; a corrected key or plant ships as a new unit tag with a new entry in `versions`, and the
  superseded entries stay registered (a result is reproducible against the exact version it was scored on).
  Some units also carry `v0.x` tags: the scripted sprint history (maturity-history, the Quellbrook estate).
- **Set versions** are tags of *this* repository (`v1.0.0`, …). A new unit version or a new unit inside the same
  rubric generation is a new set minor version; the set's `registry.json` at a set tag pins every unit's latest tag.
- **A new rubric generation is a new set repository** (`training-set-2027`, …), never a rewrite of this one: the keys
  here are labelled against `rubric-2026.10.1` and stay comparable to the results recorded against them.
- **Holdouts are private** (`holdout-2026-q4`, …): the same format (bundles, snapshots, registry, tools), kept out of
  public view so that a scanner cannot be tuned to them; results on a holdout are published, its units are not
  until it is retired.
- The units were authored as separate repositories in `github.com/code-assurance-initiative` (named as the unit) and
  consolidated here on 2026-10-07; `formerRepository` in `registry.json` records the old address. The five
  `estate-quellbrook-*` repositories remain live there as well (they back published material); the bundles here are
  the canonical copy for benchmarking.

## Units

Plants = `must-fire` entries, traps = `must-not-fire` entries, in the key at the latest tag. Family: `control` is a
certified-clean control (nothing planted, traps only); `estate` is the Quellbrook Freight reference estate (five
services of one fictional company, with Dockerfiles, Kubernetes manifests, CI, ADRs and about three sprints of
scripted history, scored at integration level); every other family names the theme of a one-theme unit.

<!-- BEGIN units (generated by tools/build_set.py) -->
| Unit | Language | Family | Theme | Plants | Traps | Latest tag | Versions |
|---|---|---|---|---:|---:|---|---|
| [`bench-csharp-baseline-clean`](units/bench-csharp-baseline-clean/) | C# | control | certified-clean control: nothing planted, traps only | 0 | 1 | v1.0.0 | v1.0.0 |
| [`bench-csharp-security-secrets`](units/bench-csharp-security-secrets/) | C# | security | hard-coded secrets and their look-alikes (incl. git history) | 17 | 24 | v1.0.0 | v1.0.0 |
| [`bench-csharp-security-dependencies`](units/bench-csharp-security-dependencies/) | C# | security | vulnerable, deprecated, outdated, copyleft and end-of-life dependencies | 8 | 8 | v1.1.0 | v1.0.0, v1.1.0 |
| [`bench-csharp-security-injection`](units/bench-csharp-security-injection/) | C# | security | injection and unsafe input handling | 20 | 23 | v1.0.0 | v1.0.0 |
| [`bench-csharp-security-iac`](units/bench-csharp-security-iac/) | C# | security | Kubernetes, Dockerfile, compose, Terraform and CI-workflow security | 19 | 20 | v1.2.0 | v1.0.0, v1.1.0, v1.2.0 |
| [`bench-csharp-architecture`](units/bench-csharp-architecture/) | C# | architecture | architecture and structure in a multi-project solution | 17 | 22 | v1.1.0 | v1.0.0, v1.1.0 |
| [`bench-csharp-domain-events`](units/bench-csharp-domain-events/) | C# | domain | domain modelling, messaging and event sourcing | 30 | 27 | v1.1.1 | v1.0.0, v1.1.0, v1.1.1 |
| [`bench-csharp-codehealth`](units/bench-csharp-codehealth/) | C# | codehealth | code health | 50 | 30 | v1.0.0 | v1.0.0 |
| [`bench-csharp-tests`](units/bench-csharp-tests/) | C# | testing | test-suite quality | 11 | 13 | v1.0.0 | v1.0.0 |
| [`bench-csharp-readiness`](units/bench-csharp-readiness/) | C# | readiness | production readiness of an API + worker | 12 | 25 | v1.0.0 | v1.0.0 |
| [`bench-csharp-maturity-history`](units/bench-csharp-maturity-history/) | C# | maturity | scripted git history (hotspots, silos, coupling), ADRs and documentation drift | 7 | 10 | v1.0.0 | v1.0.0 |
| [`bench-ts-baseline-clean`](units/bench-ts-baseline-clean/) | TypeScript | control | certified-clean control: nothing planted, traps only | 0 | 3 | v1.0.0 | v1.0.0 |
| [`bench-ts-security-secrets`](units/bench-ts-security-secrets/) | TypeScript | security | hard-coded secrets and their look-alikes | 17 | 23 | v1.0.0 | v1.0.0 |
| [`bench-ts-security-injection`](units/bench-ts-security-injection/) | TypeScript | security | injection and unsafe input handling | 21 | 25 | v1.0.0 | v1.0.0 |
| [`bench-ts-frontend-a11y`](units/bench-ts-frontend-a11y/) | TypeScript | frontend | React front-end quality and accessibility | 24 | 22 | v1.1.0 | v1.0.0, v1.1.0 |
| [`bench-ts-security-dependencies`](units/bench-ts-security-dependencies/) | TypeScript | security | npm dependency risk, incl. unused / undeclared / misplaced packages | 13 | 14 | v1.0.0 | v1.0.0 |
| [`bench-ts-codehealth`](units/bench-ts-codehealth/) | TypeScript | codehealth | code health | 51 | 36 | v1.0.0 | v1.0.0 |
| [`bench-ts-domain-privacy`](units/bench-ts-domain-privacy/) | TypeScript | domain | domain modelling, vertical slices and personal-data handling | 11 | 14 | v1.0.1 | v1.0.0, v1.0.1 |
| [`bench-csharp-blazor-a11y`](units/bench-csharp-blazor-a11y/) | C# | frontend | Blazor / Razor Pages accessibility and JS-interop correctness | 25 | 22 | v1.0.0 | v1.0.0 |
| [`bench-ts-readiness`](units/bench-ts-readiness/) | TypeScript | readiness | production readiness of an npm-workspaces monorepo on Kubernetes | 17 | 35 | v1.0.0 | v1.0.0 |
| [`estate-quellbrook-gateway`](units/estate-quellbrook-gateway/) | TypeScript | estate | reference estate: API gateway / BFF (Fastify) | 2 | 7 | v1.0.0 | v1.0.0 |
| [`estate-quellbrook-orders`](units/estate-quellbrook-orders/) | C# | estate | reference estate: order service (DDD, outbox) | 2 | 12 | v1.0.0 | v1.0.0 |
| [`estate-quellbrook-dispatch`](units/estate-quellbrook-dispatch/) | C# | estate | reference estate: dispatch service — carries the estate's regression | 5 | 15 | v1.0.0 | v1.0.0 |
| [`estate-quellbrook-notifier`](units/estate-quellbrook-notifier/) | C# | estate | reference estate: notifier worker — carries the secret left in history | 2 | 8 | v1.0.0 | v1.0.0 |
| [`estate-quellbrook-web`](units/estate-quellbrook-web/) | TypeScript | estate | reference estate: operator web front end (React + Vite) | 3 | 7 | v1.0.0 | v1.0.0 |
| **25 units** | | | | **384** | **446** | | |
<!-- END units -->

## License

MIT — see [`LICENSE`](LICENSE).
