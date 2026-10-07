# Benchmark: hardcoded secrets and their look-alikes

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its
authoring log is [`journal.md`](journal.md).

## Safety note

**Every secret in this repository is a generated fake.** Each value was produced with a cryptographically secure
random generator (Python `secrets`, `openssl genpkey`) in the real format of its kind, and authenticates nothing:
there is no account, bucket, database, partner, relay or certificate authority behind any of them. They exist so that
secret detectors have something real-looking to find. Do not report them; do not reuse them.

## Theme

A small ASP.NET Core (`net10.0`) **document-export service**: it renders an inventory report to CSV, encrypts it
(AES), uploads it to S3-compatible object storage, signs a manifest (RSA), issues short-lived download tokens (HMAC
JWT), tells a fulfilment partner's REST API and a notification relay that the export is ready, and writes an audit
row to Postgres. It ships with a release script, a migration script, dotenv files, Compose for local development,
a Dockerfile and key material. Secrets sit where real teams leak them; look-alikes sit where real teams keep
harmless values that resemble secrets.

The rest of the repository has the same scaffolding as `bench-csharp-baseline-clean` (CI pinned by commit SHA, CodeQL,
Dependabot, README, ADRs, architecture doc, CHANGELOG, central package management with lock files, nullable, `src/` +
`tests/`) so that the only signal is the theme.

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
There is **one plant per (secret type, file)**, so a scanner that reports one finding per type and file can still
score every plant.

## Plants (`must-fire`)

`API` = `src/DocumentExport.Api`.

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| SEC-001 | credential | `API/Storage/ObjectStoreOptions.cs` | Cloud access key id + secret access key as property defaults of the options class; used wherever configuration does not override them. One credential pair = one entry. |
| SEC-002 | credential | `deploy/publish.ps1` | GitHub personal access token (`ghp_`, valid checksum) passed to `dotnet nuget push`. |
| SEC-003 | crypto key | `API/appsettings.json` | `Jwt:SigningKey` — the HMAC key that signs download tokens, in the base config every environment loads. |
| SEC-004 | password | `API/appsettings.json` | Exports DB connection string with an **alphanumeric** 24-char password (no punctuation to make it "look" secret). |
| SEC-005 | password | `API/Persistence/AuditStoreSettings.cs` | **Punctuated** password in the default connection string of a `*Settings` class. |
| SEC-006 | password | `API/appsettings.Production.json` | The production DB password. |
| SEC-007 | password | `API/appsettings.Development.json` | Strong password of the team's **shared** development database on the internal network. *Contested*, see below. |
| SEC-008 | private key | `API/Keys/export-signing.pem` | PEM RSA private key with body; signs export manifests. |
| SEC-009 | private key | `src/DocumentExport.Contracts/DocumentExport.Contracts.snk` | Strong-name key **pair** (CryptoAPI PRIVATEKEYBLOB) the Contracts assembly is signed with. Binary. |
| SEC-010 | private key | `API/certs/export-api.pfx` | PKCS#12 with the TLS certificate **and private key**, empty password, served by Kestrel. Binary. |
| SEC-011 | crypto key | `API/Encryption/ExportEncryptor.cs` | AES-256 export key as `Encoding.UTF8.GetBytes("…")`. |
| SEC-012 | credential | `API/partner-api.json` | `"Authorization": "Bearer …"` in a committed JSON client configuration loaded at start-up. |
| SEC-013 | credential | `API/Notifications/DeliveryNotifier.cs` | Bearer token literal in `new AuthenticationHeaderValue("Bearer", "…")`, attached to every relay request. |
| SEC-014 | password | `deploy/migrate.sh` | `postgres://migrator:<password>@…` URI in the production migration script. |
| SEC-015 | crypto key | `.env.production` | Production `Jwt__SigningKey` in a committed dotenv file. |
| SEC-016 | credential | `API/Storage/ExportUploader.cs` | Access key pair pasted into a `//` debugging comment. |
| SEC-017 | history | `API/appsettings.Staging.json` | Staging DB password committed in one commit and removed in the next; never rotated, still in history. Commit SHAs in the key entry and the journal. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | credential | `API/appsettings.Production.json` | `__OBJECTSTORE_ACCESS_KEY_ID__` / `__OBJECTSTORE_SECRET_ACCESS_KEY__` — release-pipeline substitution tokens. |
| TRP-002 | credential | `API/appsettings.json` | `TenantId` / `ClientId` GUIDs of the identity provider — public identifiers. |
| TRP-003 | credential | `API/appsettings.json` | `SecretRotationJobId` — a GUID under a *Secret*-named property; the id of the rotation job. |
| TRP-004 | credential | `docker-compose.yml` | `Bearer ${PARTNER_API_TOKEN}`, `${OBJECTSTORE_…}` — Compose interpolation from an untracked `.env`. |
| TRP-005 | password | `docker-compose.yml` | `POSTGRES_PASSWORD` of a throwaway local container bound to 127.0.0.1. *Contested*, see below. |
| TRP-006 | password | `docker-compose.yml` | `Password=postgres` connection strings to that same local container. *Contested*, see below. |
| TRP-007 | password | `API/Persistence/ExportsDataSourceFactory.cs` | `const string PasswordVariable = "EXPORTS_DB_PASSWORD"` — a password-named constant holding a variable NAME. |
| TRP-024 | password | `API/Persistence/ExportsDataSourceFactory.cs` | `Environment.GetEnvironmentVariable(PasswordVariable)` — read at run time. |
| TRP-008 | crypto key | `API/Tokens/DownloadTokenService.cs` | `new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey))` — key from configuration. |
| TRP-009 | credential | `tests/DocumentExport.UnitTests/Storage/FakeCredentials.cs` | Random AKIA-format pair used only in in-process unit tests. |
| TRP-010 | credential | `tests/fixtures/objectstore.json` | Random AKIA-format pair in integration-test fixture config; the object store is faked. |
| TRP-011 | credential | `Dockerfile` | Base images pinned by sha256 digest (64 hex). |
| TRP-012 | credential | `API/packages.lock.json` | NuGet `contentHash` values (base64 SHA-512). |
| TRP-013 | credential | `API/Storage/ObjectStoreClientFactory.cs` | The vendor-documented example key id `AKIAIOSFODNN7EXAMPLE` in a doc comment. |
| TRP-014 | private key | `API/Keys/export-signing.pub.pem` | `BEGIN PUBLIC KEY` — the public half of the manifest key. |
| TRP-015 | private key | `deploy/tls/export-api.crt` | X.509 certificate only, no key. |
| TRP-016 | private key | `src/DocumentExport.Contracts/DocumentExport.Contracts.PublicKey.snk` | Public-only strong-name blob (PUBLICKEYBLOB). |
| TRP-017 | credential | `docs/operations.md` | A random key-id-shaped value quoted in prose to show the format. |
| TRP-018 | credential | `.env.example` | Format-realistic random key pair in the dotenv template. *Contested*, see below. |
| TRP-019 | credential | `.env.example` | `YOUR_API_KEY_HERE`. |
| TRP-020 | credential | `API/appsettings.example.json` | Format-realistic random key pair in the settings template. *Contested*, see below. |
| TRP-021 | crypto key | `API/appsettings.example.json` | `"SigningKey": "<your-key>"`. |
| TRP-022 | password | `API/appsettings.example.json` | `Password=<your-db-password>`. |
| TRP-023 | credential | `API/appsettings.example.json` | `"Authorization": "Bearer <partner-sandbox-token>"` — placeholder in the header shape SEC-012 leaks for real. |

## Contested truths, and how they were decided

- **SEC-007 — a password in `appsettings.Development.json` is a defect.** The file name says who uses the value, not
  what it protects. This one opens a shared database on the internal network that the whole team (and anything that
  compromises a team laptop or CI) can reach, and that holds a copy of real inventory data. A tool that skips
  `*.Development.*` files by name misses a real leak.
- **TRP-005/006 — `postgres`/`POSTGRES_PASSWORD` in `docker-compose.yml` are not defects.** The container is created
  from scratch by `docker compose up`, publishes its port on `127.0.0.1` only, holds only migration seed data and is
  used by nothing else. The value protects no asset; changing it to something random would add no security. Had the
  port been bound to all interfaces, or the same password been reused for a shared database, the answer would flip.
- **TRP-018/TRP-020 — format-realistic fakes in `.example` templates are not defects.** Every value in this
  repository authenticates nothing, so "is it fake?" cannot be the criterion. The criterion is what the file *is*:
  a template that no process reads (the `.example.json` is excluded from build output; `.env.example` is copied by a
  human to an untracked `.env`), whose values show the expected shape. A reviewer would ask for an obvious
  placeholder as a matter of hygiene, not report a leak. A scanner that cannot tell a template from configuration is
  caught by the trap.
- **SEC-013 — the code-shaped bearer token is the same defect as SEC-012.** The literal is a credential whether it
  sits in a JSON header map or in the arguments of `AuthenticationHeaderValue`; only the syntax differs.
- **SEC-001 / SEC-016 — one entry per credential pair.** An access key id alone is an identifier; the secret access
  key is the secret. A pair is one credential, so one entry spans both lines.

## What is clean

Every other source file is certified free of committed credentials, passwords, cryptographic keys and private keys
(`CLN-*`, concepts `hardcoded-credential`, `hardcoded-password`, `hardcoded-cryptographic-key`,
`committed-private-key`). `secret-in-version-history` is deliberately not part of the clean regions: history is not a
property of a working-tree region. `appsettings.Staging.json` has no clean entry because its earlier version is the
site of SEC-017.

## What this repository does not cover

Kubernetes `Secret` manifests (they belong to `bench-csharp-security-iac`), injection, dependency advisories, and
code-health concepts. Findings of concepts the key does not cover are reported by the harness as *uncovered*, not as
noise; an accidental real defect of another kind is fixed in the repository during authoring (see the journal).
