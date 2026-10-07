# Authoring journal — bench-ts-domain-privacy

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Purpose: close the TypeScript coverage gaps of the matrix (no frozen TypeScript measuring label for DM1, DM2, DM6,
  DM7, DM9, DM10, DM11, C1, C3, C4, C5, AX7, D23). Wrote `benchmark/answer-key.json` and `benchmark/README.md` before
  any code: 11 `must-fire` (one realistic site per concept), 13 `must-not-fire`, 5 `score-band`, and 107 planned
  `clean` files. Schema 1.2. Validated with `python3 -m cai_bench validate`: OK (136 entries).
- Concepts: the nine finding concepts of the gap rows (`cross-aggregate-object-reference`,
  `primitive-entity-identifier`, `domain-depends-on-infrastructure`, `repository-for-non-aggregate`,
  `scattered-domain-rule`, `multi-aggregate-transaction`, `constructible-invalid-entity`, `cross-slice-coupling`,
  `boundary-type-leakage`) as plant + trap + clean; the four compliance postures (`data-encryption-controls`,
  `audit-trail`, `data-retention-policy`, `data-subject-rights`) and `personal-data-inventory` as score bands; plus
  `sensitive-data-in-logs` and `consent-not-checked` as plant + trap, because a privacy-sensitive service has those
  located defects and a band alone cannot say whether a scanner found the site.
- Harness change (scanner-benchmark fff4381): concept `consent-not-checked` added — personal data used for a
  consent-based purpose on a path that never checks the consent. No taxonomy concept covered it; no rule of the
  reference scanner locates it (census in `mappings/watchdog-build/concepts.py`), so it is `unmapped` there.
- `lines` are planned positions: the key is generated from a site table with line markers and resolved against the
  code once it exists; `clean` entries will be regenerated from the tracked files.
- Score bands chosen from intent before any scan (README). `personal-data-inventory` is judged by a model and, in the
  reference scanner, composed only with a compliance framework configured: the headline is the default configuration,
  and a second run with the framework switched on is recorded as a secondary configuration (coordinator's decision).
- Decisions recorded in `benchmark/README.md` ("Contested truths").

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service: `src/shared-kernel` (aggregate root, entity, value object, identifier, domain event, money,
  result), `src/platform` (knex/PostgreSQL with three migrations, HTTP plumbing reused from the TypeScript baseline,
  AES-256-GCM field cipher, HMAC pseudonyms, audit log, outbox and in-process bus), `src/membership` (Member,
  ClassSession, ClassBooking; ten slices) and `src/billing` (BillingAccount, Invoice with InvoiceLine children; six
  slices). Express 5, knex 3, zod 4, pino 10, jose 6, TypeScript 6.0, ESLint 10 strictTypeChecked, vitest 5.
  `npm run lint`, `typecheck`, `build`, `format:check` clean; 75 tests pass against the real migrations on pg-mem;
  coverage 97 % lines / 90 % branches.
- Key changes against the draft, none weakening a plant:
  - CON-001 / TRP-013: the e-mail and SMS sends sit in two private methods, so the trap (e-mail reminder, contract
    basis) and the plant (SMS without the consent check) are more than the line tolerance apart.
  - TRP-001 anchors the constructor parameter property `sessionId`; TRP-007 ends at the booking's save;
    TRP-010 is the erasure handler's unit-of-work port (the slices receive the context's `MembershipTransactions`,
    not single repositories); TRP-009's rationale names the guarded `AccountHolder`.
  - Not certified either way for `boundary-type-leakage`: `billing-account-repository.ts` and
    `billing-account-store.ts`, where BTL-001's `MemberId` spreads into the lookup by member.
  - Slices are `features/<slice>/` folders; the billing slices are named `issue-invoices` and `charge-late-fees`
    (plural: one run covers every account), plus `record-payment`.
  - Accidental defects avoided while writing (not plants): outbox rows with personal data are deleted once delivered
    instead of being marked dispatched; DATE columns are read as text so a calendar day is not shifted by the server's
    time zone; route parameters are validated as UUIDs before they reach the domain.
- `clean` entries generated from the tracked files (139). Every `lines` entry resolved by marker and printed with its
  text for a visual check; the key validates (168 entries: 11 must-fire, 13 must-not-fire, 5 score-band, 139 clean).

## 2026-10-07 — scan iteration 1 (contained, repository commit 28b737a), judged

- Scanner: the reference scanner at engine commit 6a05dfb6c (rubric-2026.10.1), contained mode; 30 results; SARIF
  sha256 `0fb4d865dc70bb7a42b3d9c4915f4d016e49f4eb0d43fe90a57cce618aac9bf7`. Harness: **4 TP, 7 FN**, 13/13 traps
  held, no clean-region hit, 4 noise (the D23 rows below), 22 uncovered. Bands: C1 65 (BND-001 in), C3 75 (BND-002
  in), C4 50 (BND-003 in), C5 40 (BND-004 in, at the floor); LA1 unscored (default configuration).
- **Found:** AGG-001 (DM1 at the `member` property), STI-001 (DM2 at `accountId`), CSC-001 (AX7 at the reminder
  handler's declaration), LOG-001 (D32 personal-data-in-log at the SMS warning).
- **Found in substance, unmatched (harness):** BTL-001 — D23 reported `MemberId (membership → billing)` via
  `BillingAccount.memberId` and `BillingAccount.constructor`, and its spread via `BillingAccountRepository.findByMember`
  and `KnexBillingAccountRepository.findByMember` (the two files the key leaves uncertified). Every D23 row is
  location-less (type and member only in prose), so under contract 1.4 none matches the located plant: the four rows
  count as noise and BTL-001 as an FN. The rows are true; the scanner located nothing.
- **Valid, repository fixed:**
  - D29 `gcm-no-tag-length` at `field-cipher.ts:29`: `createDecipheriv` in GCM mode without `authTagLength` accepts a
    truncated tag (Node accepts 4-byte tags), weakening the integrity the cipher promises. Both cipher and decipher now
    fix the tag length at 16 bytes; a test proves a 4-byte tag is refused.
  - X4 interpolated log messages in both reminder gateways: the values now go beside constant messages. In the SMS
    gateway the phone number stays in the log entry (LOG-001 is unchanged in substance: a structured field is still
    the number in clear).
  - R10 duplication across slices and the two gateways: one `postToProvider` helper for the provider calls, one
    `idRoute` helper for routes on an id path parameter, and `existingMember` for the load-or-404 of a member. The rest
    of the per-slice repetition (handler shapes, the migration file header) is the trade-off ADR 0001 records.
- **Traps promoted:** DM4 "Anemic entity: InvoiceLine" — false-positive: the line is an immutable child entity whose
  rules live on its aggregate root (`Invoice.addLine` / `issue`), not in a service. **Key change:** TRP-014
  (`anemic-domain-model`) at the class declaration. The concept is otherwise not certified.
- **Noise recorded (uncovered, code kept):**
  - DM4 "Anemic entity: ClassSession" — opinion-not-fact: no mutators is accurate; "the business logic lives in a
    service" is not (its queries `hasStarted` / `hasRoomFor` carry the class's rules; what it lacks is validation,
    which is CIE-001). Counted as noise from iteration 2 on, now that the key covers the concept.
  - X10 "Duplicated predicate" over `member.status` / `membershipEndsOn` in `book-class.ts` and
    `send-class-reminders.ts` — valid: it is SDR-001 seen through the duplicated-predicate lens. The harness maps X10
    to no domain concept, so the repository-level plant stays an FN (coordinator: X10 rows over a domain type's members
    evidence `scattered-domain-rule`).
  - C3 "Partial audit-trail evidence" — valid (Billing's personal data is unaudited); the score is in band.
  - C4 "Partial data-retention evidence — missing: an expiry limit, a scheduled purge job" — false-positive: the purge
    job exists (`features/purge-lapsed-members`, run by `jobs.ts`) with a declared limit (`MEMBER_RETENTION_MONTHS`);
    the scanner reads neither name. The real gap (the reminder log is never purged) is what BND-003 says.
  - C5 "Partial data-subject-rights support: erasure ✓ · export ✗ · consent ✗" — false-positive on the ticks: export
    (`features/export-member-data`) and recorded consent (`features/record-consent`, `Member.hasConsented`) exist; the
    check matches other identifiers. The real gap is consent enforcement (CON-001).
  - C2 "No named authorization policies" — opinion-not-fact (as in the TypeScript baseline). D26 project cohesion 0/10
    for a 3.6k-LoC single-package service — opinion-not-fact. P6 "changelog thin" for a first release —
    opinion-not-fact.
- **False negatives (7), each re-verified real and correctly placed:** DIB-001 (the TypeScript DM6 arm reads ORM
  decorators only, not imports or queries), REP-001 (DM7 on TypeScript cannot read repository type arguments),
  SDR-001 / MAT-001 / CIE-001 (DM9, DM10, DM11 have no TypeScript arm), BTL-001 (found, location-less, above),
  CON-001 (no rule; `consent-not-checked` is unmapped). The key is not weakened for any of them.

## 2026-10-07 — scan iteration 2 (repository commit a1e3218, final), model-judged passes, freeze

- Contained pass: 20 results; SARIF sha256 `e39719617dcc02398481064031814ea0301c2b26b872b9fbba47b712a9aa855a`.
  Harness: **4 TP, 7 FN** (as in iteration 1), trap resistance 13/14 (TRP-014 caught — the DM4 InvoiceLine row, now a
  labelled trap), no clean-region hit, noise 6 of 10 (the four location-less D23 rows of BTL-001 and the ClassSession
  DM4 row, both judged in iteration 1; and TRP-014). Bands as in iteration 1: C1 65, C3 75, C4 50, C5 40 — all in;
  LA1 unscored.
- Every iteration-1 fix took effect: no D29 GCM row, no X4 rows, R10 down from ten rows to three. The remaining three
  are judged opinion-not-fact and kept: the cross-directory roll-up (10 clone groups, none reported individually),
  two route registrations differing in path and scope (`cancel-membership` / `erase-member`), and the knex migration
  file header (`import type { Knex }` + `export async function up`), which the migration format requires.
- Model-judged passes (`--host --with-llm`), run once each: the default configuration (SARIF
  `b5bc0a59d8006f7635f1730bbb2b7dc8e225bac8d9e91f567573fa8acb0bc3da`) and, as the SECONDARY configuration,
  `CODEHEALTH_COMPLIANCE_FRAMEWORKS=gdpr-tech` (the value the personal-data inventory card is gated on) — byte-identical
  SARIF. Neither produced an LA1 card, so BND-005 is unscored in both configurations. In host mode on this box the
  TypeScript sidecar exits at start (`ERR_MODULE_NOT_FOUND`: its node modules are not installed beside the host-built
  engine), so the host passes have no TypeScript code model at all: no DM, AX7 or D23 card, and no D32 (semgrep is
  contained-only), hence recall 0/11 in both host passes. LA1 reads that same code model. The C1/C3/C4/C5 scores are
  identical to the contained pass; the model-judged M4 100, D19 90 and D21 100 raised no row. Headline numbers are the
  contained default-configuration pass.
- Converged: every unexpected result is judged and either fixed (iteration 1) or recorded; the key matches the code.
  False negatives (7), unchanged and each re-verified: DIB-001, REP-001, SDR-001, MAT-001, CIE-001, BTL-001 (found,
  location-less), CON-001. Frozen as v1.0.0 with this entry.

## 2026-10-07 — v1.0.1: the boundary-leak plant names its member (contract 1.2 subject)

- The reference scanner reports boundary type leakage per exposed member and without a SARIF location (its message
  names the leaking type and the member), so v1.0.0 could only score BTL-001 as a miss. **Key change:**
  BTL-001 `subject: BillingAccount.memberId`, the whole-token form the scanner prints for the planted property.
  Code, ids, labels and lines unchanged. The rows on the repository interface and its knex implementation restate
  the same leak as it spreads (judged redundant); the harness counts them as noise because those files are
  deliberately not certified for this concept.
