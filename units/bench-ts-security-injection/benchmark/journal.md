# Authoring journal — bench-ts-security-injection

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` (schemaVersion 1.2) before any code: 21 `must-fire` (SQL-001…002, NOSQL-001,
  CMD-001, PATH-001, SSRF-001, XXE-001, DESER-001, CODE-001…002, PROTO-001, XSS-001…002, REDIR-001, REDOS-001,
  LOG-001, HASH-001, PII-001…003, REC-001), 24 `must-not-fire` (TRP-001…024), 54 planned `clean` files (CLN-*),
  3 `not-applicable` (LDAP, XPath, untrusted length fields), 1 `score-band` (BND-001, web-security posture).
- Coverage follows the matrix rows that name this repository: D29 (SAST), D32 (personal data), X17 (uncapped document
  recursion), X24 (document value in markup) as plants + traps + clean; S1 as a score band. The method mirrors the
  frozen C# counterpart `bench-csharp-security-injection` (same concepts where both languages have them, the same
  contested truths), plus the JavaScript-specific defects: NoSQL operator injection, prototype pollution, `vm` /
  `new Function` evaluation, DOM XSS and personal data in browser storage, and log forging (which the C# repository
  could not cover: the taxonomy had no log-injection concept then).
- Harness change (scanner-benchmark f933b50): two concepts did not exist and were added, `nosql-injection` (CWE-943)
  and `prototype-pollution` (CWE-1321), with the D29 rows that already denote them moved out of `offConcept`.
- Decisions recorded before the code (`benchmark/README.md`, "Contested truths"): MD5 ETag is a trap; a keyed-HMAC
  pseudonym in a log is a trap; unbounded recursion over a parsed JSON document is a plant even though Node.js turns
  stack exhaustion into a catchable error; MD5 password storage is labelled with the precise concept
  `insufficient-password-hashing`.
- `lines` are planned positions (placeholders); every one is resolved from the final code in step 2.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK, 103 entries.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service: Express 5 + zod 4 + pino 10 / pino-http + helmet + jose on Node.js 22, TypeScript 6.0
  (`strict`, `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`), node-postgres, knex, the MongoDB driver,
  libxmljs2, fast-xml-parser, js-yaml 4, Nunjucks, bcryptjs; a browser upload page under `web/` compiled with its own
  tsconfig. 3,020 non-blank lines of production code, 2,486 of tests; 187 tests green (unit, HTTP-level through
  supertest, browser modules under happy-dom), coverage 96.8 % lines / 87.5 % branches, gated in CI. `npm run lint`
  (0 errors, 3 warnings, below), `typecheck`, `build`, `format:check` clean; `npm audit` 0 vulnerabilities. No test
  touches a network or a database: stores run against a recording SQL client, a knex stand-in and in-memory
  collections; LibreOffice and ImageMagick against stand-in scripts; outbound HTTP against a fake `fetch`.
- Every plant was exercised once outside the repository against the built code to confirm it is real (not committed):
  a JSON body `{"__proto__": {...}}` through the preference merge sets a property on `Object.prototype`; ~10 KB of
  nested brackets through the metadata flattener throws `RangeError: Maximum call stack size exceeded`; `(a+)+$`
  against 28 `a` and a `!` blocks the highlighter for ~13 s; libxml with `noent: true` substitutes a `file://`
  entity, and without it does not.
- The ESLint rule `no-implied-eval` (strictTypeChecked) is demoted from error to warning in `eslint.config.js`, not
  suppressed: it reports the three `new Function` sites (CODE-002, DESER-001, TRP-011) on every lint run. No
  `eslint-disable` comment anywhere (the same approach as CA5351 in the C# repository).
- Key changes against the draft, each because the code made the site more precise (none weakens a plant):
  - Spans widened from one line to the statement range (declaration through sink) for SQL-001/002, NOSQL-001,
    CMD-001, PATH-001, DESER-001, PROTO-001, XSS-001/002, REDIR-001, LOG-001 and the traps TRP-001, 003, 005, 006,
    007, 009, 011, 012, 013, 014, 015, 021, 023, so a scanner that reports at the source or at the sink lands on the
    same entry.
  - PII-001 / TRP-021 share `report-mailer.ts`; the validator refused them within the line tolerance, so the bounce
    log moved into its own method (code commit), keeping both sites in the file.
  - The browser storage plant and trap call `localStorage` directly (the draft code took an injectable `Storage`),
    so the site names the store it writes to.
  - Rationales describe the code as written: SSRF-001's schema requires an http(s) URL; TRP-008 names DTD loading;
    REC-001 names the measured body size.
  - Clean list regenerated from `git ls-files`: every tracked file without a plant or trap (lock file, LICENSE and
    `benchmark/` aside) — 96 entries.
- All `lines` resolved from the final tree by unique markers and checked with `sed -n`; the key validates
  (145 entries: 21 must-fire, 24 must-not-fire, 96 clean, 3 not-applicable, 1 score band).

## 2026-10-07 — scan iteration 1 (contained), judged

- Scanner: the reference scanner at engine commit 6a05dfb6c (rubric-2026.10.1), contained mode, over repository
  commit a1a6358. 68 results; harness (contract 1.3): 6 TP, 15 FN, 4 traps caught, 3 clean-region FP, 1 unmatched
  result of a covered concept, 2 redundant, 52 uncovered. Score band BND-001 out (S1 80 vs 30–70).
- **Hits (6):** SSRF-001 (SSRF rule, request query to `fetch`), CODE-002 (assembled code string to `new Function`),
  XSS-001 (raw HTML format), PII-002 (personal data in URL), REC-001 (X17), XSS-002 (X24, two rows — title and
  description — the second redundant).
- **Traps caught (4) — scanner false positives, code kept:**
  - TRP-011 (assembled code string, `row-comparator.ts:25`): every hole in the comparator source is a field name
    from a fixed table keyed by the column enum, or one of two sign literals; nothing from the request reaches it.
  - TRP-015 (open redirect "default operand", `saved-search-routes.ts:88`): the message argues that an `||`/`??`
    default is not a guard; here the left operand is `sameSitePath(...)`, which returns undefined for anything that
    is not a single-slash same-site path (`src/http/local-path.ts`), so the request cannot choose another origin.
  - TRP-022 (personal data in URL, `crm-client.ts:37`): `email=` carries the literal `true`/`false`; the
    subscription is addressed by an opaque id. Same false positive as in the C# repository.
  - TRP-018 (S1 weak hash, `etag.ts:8`): MD5 as an ETag protects nothing (contested truth in the README).
- **Clean-region false positives (3):** D29 `res-render-injection` at `thumbnail-routes.ts:43`,
  `document-routes.ts:69` and `report-routes.ts:106` — **false-positive**: none of them is Express's `res.render`;
  they are `renderer.render(...)` (ImageMagick), `cards.render(...)` (a fixed template name with the document as
  data) and `reports.render(...)` (a layout applied to rows). The rule matches any `.render(` with request data in
  scope. Code kept (two of the three lines moved in the refactoring below; the claim is false at all three).
- **Unmatched result of a covered concept (1):** D29 `assembled-code-string-evaluated` at `layout-format.ts:29`
  (concept code-injection) — **valid, and it is the planted DESER-001 defect** seen through another lens: the tag
  constructor compiles the scalar an uploaded layout carries. Nothing to fix (planted). The key keeps the site as
  insecure deserialization (README, "Contested truths": what makes it a defect is that a data format carries code);
  the harness counts this row as code-injection noise. Recorded for the coordinator (a deserialization/code-injection
  family would let it count).
- **Uncovered results judged:**
  - D8 ×32 "Low coverage" (0–48 % on every route module, `app.ts`, `server.ts` …) — **false-positive**. The scanner
    installed with `npm ci --ignore-scripts`, which skips libxmljs2's install step (its prebuilt native binding), so
    every suite that imports the application fails to load. With a normal install the suite covers 96.8 % of lines
    and is gated in CI. Code kept; recorded.
  - R6 "The declared test suite includes a file that cannot be loaded: `src/documents/card-renderer.ts` … imports a
    module that names no file" — **false-positive**: card-renderer is production code, loads in every test run, and
    imports only `nunjucks`, `node:path`/`node:url` and a type; the scanner read `new URL('../../views/',
    import.meta.url)` (a directory) as an import. The line changed anyway (b8ff2ae) for an unrelated reason: the
    new happy-dom accessibility test needs a file path, so the views directory is now `path.join(import.meta.dirname,
    …)`.
  - R10 duplication — **valid** for five groups: id-plus-body parsing copied across exports, shares and thumbnails;
    the not-found document lookup in two document routes; the two XML import handlers; the two YAML report
    handlers. **Fixed (d7b561a):** `parseIdWith` and `findDocument` helpers, one `xmlImport` handler factory, one
    `fromYaml` factory. **Opinion-not-fact** for two: `search-routes.ts:40/53` (the shared seven lines are the
    parse-and-search prelude every route has; the handlers differ in what they answer, and one is the XSS-001 site,
    which is not reshaped for a style finding) and `src/http/html.ts` ↔ `web/src/escape-html.ts` (ten lines in two
    separately compiled targets, server and browser, with no shared package between them).
  - D1/D2 `reportRoutes` (cyclomatic 26, cognitive 29) — **valid**: the factory held five inline handlers.
    **Fixed (d7b561a):** named handlers. D1/R2 `validateNode` (cyclomatic 16) — **valid**, **fixed** by splitting the
    group and comparison cases; the depth check and the recursion that make TRP-024 a trap are unchanged.
  - X7 `recent-recipients.ts:9` "swallows every exception into `return []`" — **valid** (uncertain resolves to
    valid): a corrupt stored value was ignored without a trace and stayed in storage. **Fixed (9b02b6b):** logged
    and discarded, here and in the view settings. PII-003 and TRP-023 unchanged.
  - AC7 "No accessibility enforcement found" — **valid**: the upload page and the document card had no automated
    accessibility check. **Fixed (b8ff2ae):** axe-core runs over the upload page and a rendered card in the test
    suite.
  - C2 "No named authorization policies" — **false-positive**, as in `bench-ts-baseline-clean`: scopes are named
    constants in `src/http/scopes.ts`, each route declares `requireScope(Scopes.x)` at registration and the README
    lists them. **Key change:** promoted to repository-level trap TRP-025 (`authorization-enforcement`).
  - D26 "1 of 1 project(s) overshoot their size bounds (3011 LoC, 151 module-visible types across 18 directories)"
    — **opinion-not-fact**: a single small service package organised by feature folders (ADR 0001); the measurement
    is right, the call to split a 3k-line service into packages is not supported by it.
  - P6 "Changelog looks stale/thin: 2 versioned entries, bar 3" — **opinion-not-fact** (one release; as in the
    baseline repositories).
- **Score band BND-001 out (S1 80 vs 30–70):** kept. S1 does not assess server controls for TypeScript; its score
  here rests on the weak-hash arm, which fired on the MD5 ETag trap and not on the MD5 password store (HASH-001), so
  80 does not describe a service that stores passwords in MD5 and sends personal data in a URL. Recorded as a
  scanner miss; the band was set before the first scan and is not moved.
- **False negatives (15), every plant re-verified as real and correctly placed:** SQL-001 (pg template literal),
  SQL-002 (knex `raw` template), NOSQL-001 (Mongo operator injection from the body), CMD-001 (`exec` with the
  format), PATH-001 (`path.join` with `req.params` then `readFile`), XXE-001 (libxml `noent`), DESER-001 (see the
  code-injection row above), CODE-001 (`vm.runInNewContext`), PROTO-001 (deep merge), REDIR-001 (`res.redirect` to
  `req.query.next`), REDOS-001 (`new RegExp` of a caller pattern), LOG-001 (plain-text audit line), HASH-001 (MD5
  password), PII-001 (address in a pino record), PII-003 (addresses in localStorage). None of the plants changed.

## 2026-10-07 — scan iteration 2 (contained), judged

- Contained pass at 417c273 (same engine). 63 results; harness: 6 TP, 15 FN, 5 traps caught (TRP-011, 015, 018,
  022 and the new TRP-025), 2 clean-region FP (`res-render-injection` at the moved `thumbnail-routes.ts:38` and
  `document-routes.ts:67`; same false positive, iteration 1), 1 unmatched (the DESER-001 code-injection lens,
  iteration 1), 2 redundant, 45 uncovered. BND-001 unchanged (S1 80, out, kept).
- Gone after the iteration-1 fixes: R6 (unloadable file), X7 (silent fallback), five of the R10 groups, D1/R2
  `validateNode`, and the iteration-1 `res-render` row at `report-routes.ts:106`.
- Changed or new, judged:
  - D1/D2 `reportRoutes` now cyclomatic 20 / cognitive 22 (was 26 / 29) — **opinion-not-fact**: the message itself
    says most of the count belongs to the named handlers declared inside the factory; each handler is a few
    statements. The count is right, the conclusion that the factory is complex is not supported by it.
  - AC7 "Accessibility checks run in tests (Verified) but aren't gated in CI" — **false-positive**: CI's "Test with
    coverage thresholds" step runs `npm run test:coverage`, i.e. vitest over `web/tests/**` including the axe test;
    a violation fails the build.
  - R10 `import-routes.ts:24` ↔ `report-routes.ts:92` (the new XML and YAML handler factories, 16 lines) —
    **valid**: the iteration-1 fix made two copies of one body-handling routine. **Fixed (this entry's code
    commit):** `src/http/text-body.ts` (`answerTextBody`), used by both.
  - R10 `export-routes.ts:20/32` (8 lines), `thumbnail-routes.ts:29` ↔ `share-routes.ts:35` (6 lines),
    `report-routes.ts:47` ↔ `subscription-routes.ts:63` (5 lines) — **opinion-not-fact**: what remains is the
    call sequence of the shared helpers (parse, look up, stop on a miss) with different follow-ups.
  - R10 `search-routes.ts:40/53`, `html.ts` ↔ `escape-html.ts`, D26, P6, D8 ×31 — unchanged, judged in iteration 1.
- Key: regenerated (new file `src/http/text-body.ts` certified clean); no label changed.

## 2026-10-07 — scan iteration 3 (final) and freeze

- Contained pass at 314f907 (same engine). 63 results; harness: 6 TP, 15 FN, 5 traps caught (TRP-011, TRP-015,
  TRP-018, TRP-022, TRP-025), 2 clean-region FP (`res-render-injection`, iteration 1), 1 unmatched (the DESER-001
  code-injection lens, iteration 1), 2 redundant (X24 second row), 45 uncovered. BND-001 out (S1 80; kept, see
  iteration 1).
- Gone: the R10 group between the XML and YAML handler factories; D1 on `reportRoutes`.
- New, judged:
  - D2 `reportRoutes` cognitive 16 (threshold 15) — **opinion-not-fact**, as in iteration 2 (the count is the
    nested handlers').
  - D15 "Hotspot: `src/reports/report-routes.ts` changed 2 times in last 90 days" — **opinion-not-fact**: the two
    changes are this authoring loop's refactorings of a file written the same day; two commits are not churn.
  - R10 "Duplication concentrated across 8 sibling directories (5 clone groups)" — **opinion-not-fact**: the summary
    of the remaining groups, each judged in iterations 1–2.
- Everything else was judged in iterations 1–2 at unchanged code. Converged: every result is an expected hit, a
  caught trap, or a judged and recorded finding; the key matches the code. Frozen as v1.0.0 with this entry.
