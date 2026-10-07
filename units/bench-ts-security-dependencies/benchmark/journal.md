# Authoring journal — bench-ts-security-dependencies

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code, from the coverage-matrix rows that name this repository (D12,
  D14, D30, D43, D44, R8 with finding labels; D36, R5, SC1 as score bands) and `taxonomy.json`: 13 `must-fire`
  (VUL-001…005, DPR-001/002, OUT-001, LIC-001, EOL-001, UNU-001, UND-001, MIS-001), 11 `must-not-fire`
  (TRP-001…011), 3 `score-band`, and `clean` entries for the planned tree. Lines are planned positions; they are
  resolved from the final files in step 2. Every entry carries a `subject` (contract 1.2).
- Every package fact was checked on 2026-10-07: advisories with `POST https://api.osv.dev/v1/query`; deprecation,
  licence, publish dates and dependency ranges from registry.npmjs.org (`npm view`); Node.js support dates from the
  nodejs/Release schedule. Before writing the key the whole planned dependency set was installed in a scratch project
  on Node.js 22.22.1, `npm audit` showed exactly the planted advisories (axios, jsonwebtoken, decode-uri-component via
  query-string, tmp), every package of the resolved tree was asked for its deprecation and licence (only
  string-similarity and crypto-js deprecated; only mupdf copyleft among shipped packages; jszip dual-licensed), and
  the packages the code will use were smoke-tested on Node.js 22 (mupdf renders a PDF page to PNG, date-fns 1.x loads
  as CommonJS).
- Decisions (see `benchmark/README.md`, "Contested truths"): the test-only vulnerable package is a plant, not a trap
  (consistent with the C# twin); licences are judged against the product's written policy (proprietary, installed on
  customer premises); outdated rows on the vulnerable plants are redundant; transitive plants are sited at their lock
  entries; the misplaced test library is sited at its declaration; malicious dependency gets traps and clean regions
  but no plant; no install-script plant (no concept in the matrix); Dependabot deliberately absent.
- Site choice forced by path matching: the harness matches a path by suffix, so a `.nvmrc` in the tool would also
  match the root `.nvmrc` trap (TRP-009). The tool therefore states its runtime once, in `engines.node`, and EOL-001
  sits there.
- Candidates rejected while choosing the package set, so plants stay single-signal: `request` (deprecated AND
  vulnerable, with vulnerable transitives), `exceljs` as the transitive carrier (its current release ships five
  deprecated transitive packages besides the vulnerable uuid 8), `http-proxy-middleware` (its transitive braces has an
  advisory with no fixed version at all), `lodash` 4.17.21 as the "patched" trap (osv.dev now lists three advisories
  against it, fixed 4.18.0), `express` 4 (its support window is ambiguous on the freeze date), `.npmrc`
  (`save-exact`) — versions are pinned exactly by hand instead.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service and the tool as planned: Express 5, zod 4, pino 10 / pino-http 11, helmet 8 on Node.js 22,
  TypeScript 6.0 (`strict`, `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`, extending `@tsconfig/node22`),
  ESLint 10 with typescript-eslint `strictTypeChecked`, Prettier, vitest 5 with v8 coverage. Service: 29 production
  files (1,295 non-blank lines), 23 test files (1,112 non-blank lines), 108 tests green offline (nock intercepts the
  geocoding provider and the carrier; every key is generated per run), coverage 96.9 % lines / 87.0 % branches against
  CI thresholds 90/85/90/90. Tool (`tools/manifest-export`, CommonJS): 3 files, 182 non-blank lines, 5 `node:test`
  tests green. `npm run lint`, `typecheck`, `build`, `format:check` clean; `node dist/main.js` without configuration
  refuses to start with a structured fatal log. Lock files written by npm 10.9.9 (the npm line Node.js 22 ships).
- Supply chain at implementation time, exactly the planted signals: `npm audit` reports axios, jsonwebtoken,
  decode-uri-component (via query-string) and tmp; the lock file records the deprecation of string-similarity and
  crypto-js; `npm outdated` lists axios, jsonwebtoken, tmp (vulnerable plants), date-fns (OUT-001), @types/jsonwebtoken
  (follows jsonwebtoken 8 — redundant with VUL-001 by decision), @types/node (TRP-008) and typescript (see below);
  `npm audit signatures` verifies all 327 packages. The tool's lock holds minimist 1.2.5 only.
- Code facts the implementation fixed: axios 0.21 and date-fns 1.x are CommonJS, so under `nodenext` the axios client
  is taken from the module's `default` property and date-fns is imported as one object (its named exports are not
  detectable by Node's CommonJS lexer); xml2js reads an empty `<statusFeed/>` as an empty string (handled, tested);
  nock's `replyWithError` with an object never rejects an axios 0.21 request (tests use a message); mupdf "repairs"
  arbitrary bytes into a document, so the rasteriser checks for the `%PDF-` header first.
- Key changes against the draft, each because the code made the site more precise or exposed a missing look-alike:
  - **TRP-012 added** (`typescript` 6.0.3, outdated look-alike): TypeScript 7 is out, but typescript-eslint 8.71 —
    the newest — declares `typescript >=4.8.4 <6.1.0`. Staying on 6.0 is a recorded decision
    (`docs/adr/0004-typescript-follows-the-linter.md`), the same shape as TRP-008.
  - VUL-004 and DPR-002 span their whole lock-file entry (key line to closing brace).
  - Clean entries regenerated for every tracked file outside `benchmark/` and the two manifests (79).
- All `lines` resolved from the committed tree by pattern and checked with `sed -n`; the key validates.

## 2026-10-07 — scan iteration 1

- Scanner: the reference scanner at engine commit 6a05dfb6c, rubric-2026.10.1, contained mode, over repository commit
  66c4126 (scanned with no `node_modules`, as a fresh clone); 9 results; `report.sarif` sha256 c1363128…c4aa8.
- Plants found (6 of 13): VUL-001 jsonwebtoken, VUL-002 axios, VUL-004 decode-uri-component (the row names the
  transitive path through query-string and the geocoding client), VUL-005 tmp (test-only) — all D30, located at line 1
  of the root lock file and matched by subject; VUL-003 minimist (D30, the tool's lock file); LIC-001 mupdf (D14,
  location-less). MIS-001 nock was also found — R8 "Test-only dependency 'nock' in production deps" — but located at the
  test file that imports it (`tests/unit/geocoder.test.ts:1`), not at the declaration, so the harness counts it as a
  hit on that test file's clean entry. **Judged: valid, an expected hit at an evidence location**; the key stays (the
  defect is the `dependencies` line; the test file is not defective), recorded as a harness/location limitation.
- Missed (scanner false negatives; every plant re-verified in the tree and on the registry):
  - DPR-001 string-similarity and DPR-002 crypto-js — the npm arm of the dependency-hygiene dimension does not grade
    deprecation (its own narrative: "Whether any of these packages is DEPRECATED ... is not graded"), although the
    lock file written by npm 10 carries a `deprecated` field for both.
  - OUT-001 date-fns — measured (findings.md lists "date-fns is pinned at 1.30.1; ... 4.4.0") but emitted at Info
    level, which never reaches SARIF (same as the C# twin).
  - EOL-001 Node.js 16 — the end-of-life dimension reads only a root version file (`.nvmrc`) for Node.js and never
    `engines.node`, and nothing in a second package (1 platform declaration read).
  - UNU-001 uuid — declared, imported nowhere; the npm truthfulness dimension reported 0 unused.
  - UND-001 ms — imported by `src/scheduling/cutoff.ts:1`, declared nowhere; reported 0 unlisted (most likely because
    `@types/ms` is declared — types are not the runtime package).
- Traps: all left alone (TRP-001…012). Off-theme results, verdicts:
  - X5 (location-less) "Strict null checking is not enabled everywhere ...: 0/2 ... configuration(s)":
    **false-positive** — `tsconfig.json` extends `@tsconfig/node22`, whose configuration sets `"strict": true`, and
    `tsconfig.build.json` extends `tsconfig.json`; `tsc --showConfig` prints `"strict": true` for both. The rule does
    not resolve `extends` (and in a fresh clone the base lives in a devDependency). **Key change:** promoted to trap
    TRP-013 (`nullable-analysis-disabled`, repository level). Code kept — writing `strict` a second time to please a
    rule would be tuning.
  - C2 (location-less) "No named authorization policies": **false-positive**, as in bench-ts-baseline-clean — the
    scopes are named in `src/http/require-terminal.ts` (`Scopes`) and every route declares its scope where it is
    registered. **Key change:** promoted to trap TRP-014 (`authorization-enforcement`, repository level).
- Score bands: BND-003 `outdated-dependency` out (D12 98, R5 93 against [30, 85]). The scanner grades only direct
  production dependencies at 0.05 per stale package; three of seventeen a major or more behind is, to a reviewer, not
  "exemplary" freshness. **opinion-not-fact** on the scale; band kept (set before the scan). BND-001 and BND-002
  unscored (SC1 publishes no score; D36 withheld).
- No `valid` off-theme finding: no repository fix. Harness after the key change: recall 6/13, trap resistance 12/14
  (TRP-013, TRP-014 caught), noise 3/9 (the two traps and the nock evidence location).

## 2026-10-07 — scan iteration 2 (converged) and freeze

- Same scanner and mode, over repository commit 3c0b6fb (iteration-1 journal and the two promoted traps; no code
  change); 9 results; `report.sarif` sha256 c1363128…c4aa8 — byte-identical to iteration 1, so the benchmark files'
  new prose changed nothing the scanner reads.
- Harness: recall 6/13, trap resistance 12/14 (TRP-013 and TRP-014 caught, as recorded), noise 3/9. Judged by hand:
  recall 7/13 — MIS-001 is found, at the importing test file rather than the declaration. Six false negatives:
  DPR-001, DPR-002, OUT-001, EOL-001, UNU-001, UND-001.
- No LLM-judged dimension carries a score band in this key, so no model-judged pass was run.
- Converged: the scan's unexpected results are recorded noise and the key matches the code. Before the tag the
  service was re-installed with `npm ci` and lint, type-check, format check, 108 tests (coverage 96.97 % lines) and the
  tool's 5 tests passed. Frozen as v1.0.0.
