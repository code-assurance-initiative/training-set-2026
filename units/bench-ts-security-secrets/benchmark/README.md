# Benchmark: hardcoded secrets and their look-alikes (TypeScript)

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its
authoring log is [`journal.md`](journal.md). It is the TypeScript counterpart of `bench-csharp-security-secrets`
and follows the same method.

## Safety note

**Every secret in this repository is a generated fake.** Each value was produced with a cryptographically secure
random generator (Python `secrets`, `openssl genpkey`) in the real format of its kind, and authenticates nothing:
there is no bucket, registry, Stripe account, Slack workspace, e-mail account, transcoding vendor, database or
Google Cloud project behind any of them. They exist so that secret detectors have something real-looking to find.
Do not report them; do not reuse them.

## Theme

A small Node.js 22 + TypeScript (strict, ESM) **media upload and notification service** on Fastify: browsers upload
media through a small Vite page that signs each upload request; the service stores the object in S3-compatible
storage, records it in Postgres, asks a transcoding vendor for thumbnails, issues short-lived download tokens (HS256
JWT), reports metered storage to Stripe, signs outbound webhooks (Ed25519), notifies through Slack, SendGrid e-mail
and Firebase Cloud Messaging, and writes an audit trail to a hosted MongoDB cluster. It ships with a release script,
dotenv files, an `.npmrc`, a REST-client request file, Compose for local development, a Dockerfile and key material.
Secrets sit where real teams leak them; look-alikes sit where real teams keep harmless values that resemble secrets.

The rest of the repository has ordinary scaffolding (CI pinned by commit SHA, CodeQL, Dependabot with a cooldown,
README, ADRs, architecture doc, CHANGELOG, a committed `package-lock.json`, strict TypeScript, `src/` + `tests/`
with real passing tests) so that the only signal is the theme.

**Deliberate omission: there is no `SECURITY.md`** (and no `security.txt`). The baseline repository has one; this
one does not, so the pair is a presence/absence variant for the vulnerability-disclosure-policy posture
(`BND-001`, band 0–10).

## Labels are about truth

A `must-fire` is something a careful human security reviewer would flag in a code review. A `must-not-fire` is a
site a careless rule would flag and that reviewer would not. Neither label was chosen to match any scanner. Where a
reviewer could reasonably argue the other way, the entry is marked *contested* below, with the reasoning that decided
it.

Concepts (from `scanner-benchmark/taxonomy.json`): `hardcoded-credential` (CWE-798), `hardcoded-password` (CWE-259),
`hardcoded-cryptographic-key` (CWE-321), `committed-private-key` (CWE-798), `secret-in-version-history` (CWE-540).
There is **one plant per (concept, file)**, so a scanner that reports one finding per type and file can still score
every plant. No planted line carries an excuse word, and the repository carries no scanner-specific suppression
marker.

## Plants (`must-fire`)

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| SEC-001 | credential | `src/storage/objectStore.ts` | S3-compatible access key id + secret access key as the `??` fallbacks of the storage config. One pair = one entry. |
| SEC-002 | credential | `scripts/release.mjs` | GitHub personal access token (`ghp_`, valid checksum) that creates the GitHub release. |
| SEC-003 | crypto key | `src/auth/downloadTokens.ts` | The HS256 download-token secret as a literal passed to `TextEncoder().encode(…)` (the fallback when `JWT_SIGNING_SECRET` is unset). |
| SEC-004 | password | `src/db/pool.ts` | `postgres://media_app:<24 alphanumerics>@…` fallback connection URI. |
| SEC-005 | password | `.env` | A committed (not ignored) `.env` whose `mongodb+srv://` audit URI carries a **punctuated** password. |
| SEC-006 | crypto key | `.env.production` | Production `JWT_SIGNING_SECRET` in a committed dotenv file. |
| SEC-007 | credential | `.npmrc` | `_authToken` (a GitHub PAT) for the organisation's package registry in the project `.npmrc`. |
| SEC-008 | private key | `keys/webhook-signing.pem` | PEM Ed25519 private key that signs outbound webhooks. |
| SEC-009 | private key | `config/firebase-service-account.json` | Google service-account key file; its `private_key` is a PEM RSA key. *Contested concept*, see below. |
| SEC-010 | credential | `src/billing/usageReporter.ts` | Stripe live-mode **restricted** key (`rk_live_`). |
| SEC-011 | credential | `src/notify/slack.ts` | Slack incoming-webhook URL (the path is the credential). |
| SEC-012 | credential | `src/notify/email.ts` | SendGrid API key (`SG.<id>.<secret>`). |
| SEC-013 | credential | `src/transcode/transcoderClient.ts` | `Authorization: Bearer …` as an axios default header. |
| SEC-014 | credential | `requests/transcoder.http` | `Authorization: Bearer …` in a committed REST-client request file (one token, on both requests of the file). |
| SEC-015 | credential | `src/media/thumbnailer.ts` | Access key pair pasted into a `//` comment. |
| SEC-016 | crypto key | `web/vite.config.ts` | Upload-signing HMAC secret injected into the **browser bundle** with Vite `define`. |
| SEC-017 | history | `src/config.ts` | Slack bot token (`xoxb-`) committed as a fallback and removed in the next commit; never rotated, still in history. Commit SHAs in the key entry (`commit`) and the journal. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | credential | `src/config.ts` | `env.STRIPE_RESTRICTED_KEY`, `env.SENDGRID_API_KEY`, `env.SLACK_BOT_TOKEN` — read at run time. |
| TRP-002 | credential | `.env.example` | `YOUR_SENDGRID_KEY`, `https://hooks.slack.com/services/<team>/<channel>/<token>`. |
| TRP-003 | credential | `.env.example` | Format-realistic random key pair in the dotenv template. *Contested*, see below. |
| TRP-004 | credential | `docker-compose.yml` | `${OBJECTSTORE_…}`, `${STRIPE_RESTRICTED_KEY}`, `${UPLOAD_SIGNING_SECRET}` — Compose interpolation. |
| TRP-005 | password | `docker-compose.yml` | `postgres` password of a throwaway local container bound to 127.0.0.1. *Contested*, see below. |
| TRP-006 | credential | `tests/fixtures/storage.json` | Random key pair in test fixture config; the store is faked. |
| TRP-007 | credential | `src/notify/__fixtures__/slackPayloads.ts` | Random webhook-shaped URL used only by unit tests with fetch stubbed. |
| TRP-008 | credential | `package-lock.json` | `integrity` SHA-512 hashes of public tarballs. Whole file. |
| TRP-009 | credential | `web/index.html` | Subresource Integrity `sha384-…` hash of a public CDN file. |
| TRP-010 | credential | `config/secret-refs.json` | UUIDs under secret-ish names: ids of secret-manager entries. |
| TRP-011 | private key | `keys/webhook-signing.pub.pem` | The public half of the webhook key. |
| TRP-012 | credential | `src/storage/s3Client.ts` | The vendor-documented example pair `AKIAIOSFODNN7EXAMPLE` / `wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY` in TSDoc. |
| TRP-013 | credential | `web/src/checkout.ts` | Stripe **publishable** key (`pk_test_`) passed to `loadStripe`. *Contested*, see below. |
| TRP-014 | credential | `web/src/firebase.ts` | Firebase web-app config (`apiKey` `AIza…`, sender/app ids) and the VAPID **public** key. *Contested*, see below. |
| TRP-015 | credential | `README.md` | An expired single-object download token as a response example; `Bearer <token>` and `${NPM_TOKEN}` placeholders. *Contested* (the token), see below. |
| TRP-016 | credential | `Dockerfile` | Base images pinned by sha256 digest. |
| TRP-017 | crypto key | `src/auth/uploadSignature.ts` | `createHmac("sha256", secret)` with `secret` from configuration. |
| TRP-018 | password | `src/config.ts` | `env.DATABASE_URL`, `env.AUDIT_MONGO_URI`. |
| TRP-019 | private key | `tests/fixtures/webhook-test-key.pem` | An Ed25519 key generated for the signer's unit tests, trusted by nothing else. *Contested*, see below. |
| TRP-020 | crypto key | `.env.example` | `UPLOAD_SIGNING_SECRET=<generate: openssl rand -hex 32>`. |
| TRP-021 | password | `.env.example` | `mongodb+srv://<user>:<password>@<cluster>/audit`, and `postgres://media:postgres@127.0.0.1…` for the local Compose database. |
| TRP-022 | credential | `tests/notify/email.test.ts` | Random SendGrid-format key asserted in a unit test with fetch stubbed. |
| TRP-023 | password | `docker-compose.yml` | `postgres://media:postgres@db:5432/media` — the API's connection string to the TRP-005 container. |

## Contested truths, and how they were decided

- **TRP-013 — a Stripe publishable key in browser code is not a defect.** Stripe issues two keys per mode for
  exactly this split: the *secret* (or *restricted*) key stays on the server, the *publishable* key is meant to be
  embedded in web and mobile clients, and Stripe's own documentation says so. A publishable key can tokenize card
  details and confirm intents the server created; it cannot read or move money. A test-mode key additionally reaches
  only test data. Contrast SEC-010 (`rk_live_`): same vendor, the secret side. A scanner that reports every `pk_` is
  caught by the trap.
- **TRP-014 — the Firebase web `apiKey` is not a defect.** It identifies the Firebase project to Google's APIs and is
  delivered to every browser that loads the app; Firebase documents that it is not a secret and that access is
  controlled by Security Rules and App Check. The same `AIza…` shape *would* be a defect as an unrestricted server key
  for a paid Google API — the shape alone does not decide, the role does.
- **SEC-009 — the service-account JSON is filed as `committed-private-key`.** The file is a credential, and the secret
  in it is an RSA private key (`private_key`, PEM). The precise concept that is true of the site is the private key;
  a scanner reporting it as a generic credential is still pointing at the right line.
- **SEC-005 — a committed `.env` is a leak even though dotenv calls it "local".** The file is tracked (`.gitignore`
  ignores only `.env.local` / `.env.*.local`), so it ships with every clone, and the URI it holds is the hosted audit
  cluster, not a container on the developer's machine.
- **TRP-005/TRP-023 (and the local line of TRP-021) — `postgres` in `docker-compose.yml` is not a defect.** The container is created from
  scratch by `docker compose up`, publishes its port on `127.0.0.1` only, holds only migration seed data and is used
  by nothing else. Had the port been bound to all interfaces, or the same password been reused for a shared database,
  the answer would flip (compare SEC-004, the same `postgres://` shape against the shared database).
- **TRP-003 — a format-realistic fake in `.env.example` is not a defect.** Every value in this repository
  authenticates nothing, so "is it fake?" cannot be the criterion. The criterion is what the file *is*: a template no
  process reads (a human copies it to an untracked `.env.local`), whose values show the expected shape. A reviewer
  would ask for an obvious placeholder as a matter of hygiene, not report a leak.
- **TRP-015 — the download token quoted in the README is not a defect.** It is a real token shape signed with the
  service's key, but it expired long ago and was scoped to one media object; a signature reveals nothing usable
  about an HMAC key. It grants nothing today. The README's other examples are explicit placeholders.
- **TRP-019 — a private key under `tests/fixtures/` is not a defect.** It was generated for the signer's unit tests,
  is trusted by no subscriber and signs nothing outside the test process. The production key (SEC-008) is the defect;
  this one is the fixture that lets the tests avoid it. A reviewer might prefer generating the key in test setup, but
  would not report a leak.
- **SEC-013 / SEC-014 — the same leak in two shapes.** A bearer token is a credential whether it is an axios default
  header in code or a header line in a saved REST-client request; the `.http` file is committed text that every clone
  reads. They are different tokens in different files, so they are two plants.
- **SEC-001 / SEC-015 — one entry per credential pair.** An access key id alone is an identifier; the secret access
  key is the secret. A pair is one credential, so one entry spans both lines.

## What is clean

Every other source and configuration file listed in the key (`CLN-*`) is certified free of committed credentials,
passwords, cryptographic keys and private keys (concepts `hardcoded-credential`, `hardcoded-password`,
`hardcoded-cryptographic-key`, `committed-private-key`). `secret-in-version-history` is deliberately not part of the
clean regions: history is not a property of a working-tree region.

## What this repository does not cover

Kubernetes `Secret` manifests (they belong to the IaC repositories), injection, dependency advisories, and code-health
concepts. Findings of concepts the key does not cover are reported by the harness as *uncovered*, not as noise; an
accidental real defect of another kind is fixed in the repository during authoring (see the journal).
