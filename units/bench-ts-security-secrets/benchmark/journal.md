# Authoring journal — bench-ts-security-secrets

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 17 `must-fire` (SEC-001…017), 22 `must-not-fire`
  (TRP-001…022), 19 `clean` files (CLN-*), 1 `score-band` (BND-001, no SECURITY.md). Schema 1.2. Lines are planned
  positions; they are fixed to the final code in the implementation step. SEC-017 gets its `commit` once the history
  pair exists.
- Method mirrors the frozen `bench-csharp-security-secrets` v1.0.0: one plant per (concept, file); the history-only
  plant is a scripted commit pair (added, then removed) recorded with the `commit` field; no excuse word on a planted
  line; no suppression markers; truth-based labels; contested truths explained in `benchmark/README.md`.
- New against the C# repository, because they are where JavaScript teams leak or look-alike: a committed `.env` and
  `.npmrc`, a Vite `define` that ships a secret to browsers, a saved `.http` request, a Slack webhook URL, a SendGrid
  key, a Stripe restricted key versus a Stripe publishable key (trap), a Firebase web `apiKey` (trap), a Google
  service-account key file, `package-lock.json` integrity hashes, an SRI hash, a `__fixtures__` module and a test-only
  private key.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK (59 entries).

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service (Node 22, TypeScript 7 strict with `noUncheckedIndexedAccess` and
  `exactOptionalPropertyTypes`, ESM, Fastify 5, vitest 5, `@aws-sdk/client-s3`, `jose`, `pg`, `mongodb`, `axios`,
  `google-auth-library`; a Vite upload page with `@stripe/stripe-js` and `firebase`). `npm ci`, `npm run typecheck`,
  `npm test` (13 files, 32 tests), `npm run build` and `npm run build:web` are green; `npm audit` reports 0
  vulnerabilities (a vulnerable `@grpc/grpc-js` that `firebase` pulled in is overridden to `^1.14.5`). The built
  bundle contains the SEC-016 secret (checked), which is the point of that plant.
- Every secret value was generated with Python `secrets` (GitHub tokens with a valid CRC32/base62 checksum;
  access key ids from the `[A-Z2-7]` alphabet; 40-char base64 secret keys; Stripe, Slack, SendGrid shapes) or
  `openssl genpkey` (Ed25519 webhook and test keys, the service account's RSA key). The VAPID public key is the public
  half of a P-256 key whose private half was discarded. The README token is a real HS256 token signed with the
  SEC-003 key, issued and expired on 2025-11-03. The SRI hash is the real sha384 of the referenced CDN file; the image
  digests are the registry's current ones. No value contains an excuse word, `EXAMPLE` or a keyboard/ascending run.
- Every secret occurs in exactly one commit and one working-tree file (checked with `git log -S` and `git grep`).
- History-only secret (SEC-017): the Slack bot token was committed as the `slackBotToken` fallback at line 27 of
  `src/config.ts` in `0d4ff97339736d3053447405f57049a915321c76` ("Nightly job: …") and removed in
  `dcfb3f3616a74db010c6fe98a5c339412b7fd956` ("Slack digest: take the bot token from SLACK_BOT_TOKEN only"). The key
  entry carries `commit: 0d4ff97` (contract 1.1).
- Key changes against the draft, each because the code made the site more precise:
  - SEC-001/SEC-003 rationales: the literals are `??` fallbacks of environment variables, not bare defaults.
  - SEC-014 spans lines 6–16: the file's two saved requests carry the same token (one plant).
  - TRP-001/TRP-018 name the actual `env.X` lookups; TRP-004 the actual Compose variables.
  - TRP-014 widened to the whole Firebase web config plus the VAPID public key (all public identifiers).
  - TRP-021 widened to the local Compose `DATABASE_URL` line of `.env.example` (same truth as TRP-005).
  - TRP-023 added: the API container's `postgres://media:postgres@db…` connection string in `docker-compose.yml`.
  - Clean list rebuilt from the final tree: 47 files (every tracked file except plant/trap files, `LICENSE`,
    `README.md` and `benchmark/`). `src/auth/uploadSignature.ts` and `src/storage/s3Client.ts` left the clean list
    because they are trap sites.
- All `lines` were resolved from the final tree (SEC-017 from its commit) and checked with `sed -n` / `git show`; the
  key validates (88 entries: 17 must-fire, 23 must-not-fire, 47 clean, 1 score-band).

## 2026-10-07 — scan iteration 1 (judged)

- Scanner: the reference scanner at engine commit 6a05dfb6c, rubric-2026.10.1, contained mode, over repository commit
  d4a6e11; 46 results. Full verdicts: `cai-bench/_scans/bench-ts-security-secrets/iter1.verdicts.md`.
- Expected hits: 12 of 17 plants (SEC-001, 002, 003, 007, 008, 009, 010, 011, 012, 013, 015, 017). SEC-017, the
  history-only Slack bot token, was found by the history pass at exactly its commit (`0d4ff97`). SEC-003 was found
  only by a SAST rule on the `process.env.X ?? "literal"` shape. Nine further rows restate found plants (redundant).
- **Trap caught (1):** TRP-010 — secret-manager entry UUIDs under `…SecretId` / `webhookSigningKeyId` names reported
  as a signing key (false positive). Every other trap held, notably the Stripe publishable key, the Firebase web
  config, the AWS documentation pair, the README's expired token, the test-only private key, SRI and lock-file hashes.
- **False negatives (5, each re-verified as a real plant; key unchanged):** SEC-004 (`postgres://` URI with an
  alphanumeric password), SEC-005 (punctuated password in a `mongodb+srv://` URI in `.env`), SEC-006 (unquoted
  base64 signing secret in `.env.production`), SEC-014 (bearer token in a `.http` request file), SEC-016 (HMAC secret
  shipped to browsers through Vite `define`).
- **Valid off-theme findings, so the repository was fixed (fix forward, code was pushed):**
  1. The download-token and plan-upgrade endpoints trusted a bare `x-owner-id` header (surfaced by the "thin
     authorization" row): both now require the request signature — `00c3619`, which also split the upload route
     handlers that the complexity rows flagged.
  2. Outbound HTTP calls had no timeout (release script row): every fetch and the FCM request are now bounded — `e58ee43`.
  3. `.npmrc` had no minimum release age: `min-release-age=7` — `b16f6ac` (appended; SEC-007 keeps line 2).
  4. No coverage provider, no lint script, untested adapters: `@vitest/coverage-v8`, typescript-eslint
     (`recommendedTypeChecked`; TypeScript moved from 7.0 to 6.0 because typescript-eslint supports `<6.1`), tests for
     the repository, pool, transcoder, thumbnailer, S3 client and push adapters (45 tests) — `0d76033`.
- **Recorded as noise, code kept:** no named authorization policies, thin changelog, and "no test reaches this file"
  for composition roots / release script / browser glue (opinion-not-fact); "dead file" for the nightly entry point
  (false-positive).
- Key changes: TRP-022 moves to line 9 (a helper import was added above it); eight new files join the clean list
  (`eslint.config.js` and the new tests). All lines re-checked; the key validates (96 entries).

## 2026-10-07 — scan iteration 2 (judged)

- Same engine build, repository commit de8b5e3; scored with the current harness. Secret concepts unchanged: recall
  12/17, trap resistance 22/23 (TRP-010 again, unchanged code), the same five verified false negatives. Every
  iteration-1 valid finding is gone. Verdicts: `cai-bench/_scans/bench-ts-security-secrets/iter2.verdicts.md`.
- **Valid, repository fixed:** low coverage of the MongoDB audit log and the S3 store (tests, `22d9d39`); coverage
  collected but not gated (vitest thresholds 80/80/85/75, CI fails below them, `8278fa1`).
- **Recorded as noise:** 0 % coverage of the two composition roots and "only 1 place refuses" after the check was
  centralised (opinion-not-fact).
- Also: Dependabot opened Node 26 and `@types/node` 26 major bumps; the service targets Node 22 LTS, so
  `dependabot.yml` now ignores those majors and the two PRs were closed.
- Key: two new test files join the clean list (98 entries). No label changed.

## 2026-10-07 — scan iteration 3 (final) and freeze

- Same engine build, repository commit 4892fd0. 30 results; every secret-concept result is an expected hit or
  redundant to one, except TRP-010 (unchanged code, not re-judged). The iteration-2 valid findings are gone. Remaining
  off-theme rows are the ones already recorded as noise (composition-root coverage, centralised authorization
  counted as one place, no named policies, thin changelog); their code is unchanged, so they are not re-judged.
- Outcome on the secret concepts: recall 12/17, trap resistance 22/23, noise 1/22. False negatives unchanged and
  each a verified real plant: SEC-004, SEC-005, SEC-006, SEC-014, SEC-016. The key is not weakened.
- BND-001 (no SECURITY.md): unscored — the scanner treats the missing policy as not applicable rather than scoring
  it. Band kept as written.
- Converged: the repository holds exactly what its key says. Frozen as v1.0.0 with this entry.
