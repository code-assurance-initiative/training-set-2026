# Authoring journal — bench-csharp-security-injection

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 20 `must-fire` (SQL-001…003, CMD-001, PATH-001, SSRF-001…002,
  XXE-001, XPATH-001, DESER-001, XSS-001…002, REDIR-001, LDAP-001, REDOS-001, HASH-001, PII-001…002, LEN-001,
  REC-001), 22 `must-not-fire` (TRP-001…022), 18 `clean` files planned (CLN-*), 1 `not-applicable` (NA-001,
  code injection), 3 `score-band` (BND-001…003: authorization, data protection, input-validation posture).
- Coverage follows the matrix rows that name this repository: D29 (SAST), D32 (personal data), X14 (bypassable
  address classification), X15 (untrusted length), X17 (uncapped document recursion), X24 (document value in markup)
  as plants + traps + clean; C1, C2, S1 as score bands.
- `lines` are planned positions (placeholders); every one is resolved from the final code in step 2.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK. The validator rejected a first draft in
  which plants and traps that share a file had the same placeholder line; the draft placeholders were separated.
- Decisions recorded in `benchmark/README.md` ("Contested truths"): an MD5 ETag is a trap; a keyed-HMAC pseudonym in a
  log is a trap; a recursive `JsonNode` walk is a trap because System.Text.Json caps depth at 64, while the same
  shape over `XElement` is a plant. Log forging is not covered (no taxonomy concept; non-structured logging belongs to
  the code-health repository).

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the service (ASP.NET Core `net10.0`, controllers, Dapper + `NpgsqlCommand` + EF Core on Npgsql,
  Newtonsoft for two file formats, System.Xml, System.DirectoryServices.Protocols, LibreOffice/ImageMagick child
  processes) and two test projects (xUnit v3): `dotnet build ReportDesk.slnx -c Release` 0 errors (2 CA5351
  warnings, see below), `dotnet test` 74 unit + 18 integration tests passed. No test touches a network: every store,
  the directory, the mail relay and the CRM are in-memory fakes.
- Key changes against the draft, each because the code made the site more precise (none weakens a plant):
  - Folder renames forced by the compiler: `Directory/` → `People/` (the namespace shadowed `System.IO.Directory`),
    `Imports/` → `Importing/` (CA1716, reserved keyword). LDAP-001, TRP-016, LEN-001, TRP-021 moved with them.
  - HASH-001 is `MD5.HashData(...)` rather than `MD5.Create()` (CA1850 rejects the instance form); still unsalted
    MD5 for a password.
  - PII-001 / TRP-019: the analyzers require source-generated `LoggerMessage` logging (CA1848), so the template lives
    on a partial method and the keyed site is the call that passes the value (`subscriber.EmailAddress`, resp. the
    HMAC pseudonym `emailAddressHash`).
  - CA5351 (broken crypto) is demoted from error to warning for the API project (`WarningsNotAsErrors`), not
    suppressed: both MD5 sites keep producing a compiler warning. No `#pragma`/`SuppressMessage` anywhere.
  - XPATH-001's example payload rewritten for the predicate as built (restricted fields); SSRF-002's rationale now
    names NAT64 as the form that actually connects inward; TRP-005 and TRP-014 rationales describe the code as
    written (no `--`; `NumberStyles.None`).
  - Spans widened from one line to the statement range (declaration through the sink) for SQL-001/002/003, CMD-001,
    PATH-001, XXE-001, DESER-001, LDAP-001, REDOS-001, LEN-001 and the corresponding traps, so a scanner that reports
    at the source or at the sink lands on the same entry.
  - Clean list generated from the tree: every file under `src/` and `tests/` without a plant or trap (lock files
    aside, `src/.editorconfig` included), both workflows and the two props files — 86 entries. `WebhookDispatcher.cs` deliberately left
    unlabelled (its safety is SSRF-002's).
- All `lines` resolved from the final tree by unique markers and checked with `sed -n`; the key validates.

## 2026-10-07 — scan iteration 1 (contained), judged

- Scanner: the reference scanner at engine commit 6a05dfb6c (rubric-2026.10.1), contained mode, over repository
  commit ebf8cf2. 52 results; harness: 10 TP, 10 FN, 3 traps caught, 0 clean-region FP, 36 uncovered (26 of them
  per-file coverage rows). Score bands: C2 100 (BND-001 in), C1 30 (BND-002 in), S1 40 (BND-003 in).
- **Hits (10):** SQL-003 (csharp-sqli on `CommandText`), PATH-001 (unsafe-path-combine), XPATH-001, XXE-001 (two
  rules at one site: the second is redundant), LDAP-001, PII-002 (personal-data-in-url), SSRF-002 (X14), LEN-001
  (X15), REC-001 (X17), XSS-002 (X24, attribute and element-text rows: the second is redundant).
- **Traps caught (3) — scanner false positives, code kept:**
  - TRP-008 (X14 on `FeedAddressPolicy`): the message says the IPv6 branch "classifies the address as written" so
    `::10.0.0.1`, `2002:0a00:0001::` and `64:ff9b::10.0.0.0` "take the opposite verdict". Disproof: lines 17–20 return
    `false` for every IPv6 address that is not IPv4-mapped, so all three get the same verdict as `10.0.0.1`. The
    detector looks for the embedding prefixes, not for a branch that rejects everything else.
  - TRP-010 (D29 xpath-injection on `MetadataSectionReader`): the value passed `XmlConvert.VerifyNCName` on line 13
    and an NCName cannot alter the expression. The rule has no sanitiser for it.
  - TRP-020 (D32 personal-data-in-url and S1 "request payload sent in the URL query string" on
    `SubscriberDirectoryClient.cs:17`): `email=` carries the literal `true`/`false`. Both rows are one logical finding
    (same site, same claim) and false: no personal data is in that URL.
- **Unexpected results judged:**
  - D17 `.editorconfig:37` (CA2007 off, "only record is this line") — **false-positive**: the reason is on line 36 and
    `src/.editorconfig` re-enables the rule (same finding as in the baseline repository). **Key change:** promoted to
    trap TRP-023 (`suppressed-diagnostic`).
  - S1 `Shares/ShareLink.cs:4` "Password hashing without a KDF" — **valid**, and it is the planted HASH-001 defect
    (MD5 share-link passwords), reported at the `PasswordHash` property under the scanner's password-storage check
    rather than at the hasher. Not an accidental defect, so nothing to fix; scored `uncovered` (concept
    `insufficient-password-hashing`) and HASH-001 stays a false negative of the weak-hash checks. Key kept: the plant
    is where a reviewer would point (the MD5 call), and moving it to meet the scanner would be tuning the key.
  - C1 "No data-protection/encryption" (repository level) — **valid**, and it is the posture BND-002 records (band
    10–50, scored 30). Deliberate; not changed.
  - X6 `ImportsController.cs:55` "hand-rolled JSON/XML parsing via regex" — **valid** (uncertain resolves to valid):
    the preview scraped `<title>` out of arbitrary HTML with a regex. **Fixed** (76abc20): the preview returns status,
    content type, size and Last-Modified; no body parsing. SSRF-001 is unchanged (the server still connects anywhere
    and returns what it learned); its rationale now names those fields.
  - X7 `DocumentCardRenderer.cs:23` "silent fallback to 0 on parse failure" — **valid**: an unparseable page count was
    rendered as "0 pages", a false statement. **Fixed** (c9d1feb): the footer is omitted when the count is unknown,
    documented on the member. TRP-014 (the parsed integer in the attribute) is unchanged.
  - P7 "outbound HTTP without resilience" — **valid** for the partner feed and the CRM. **Fixed** (ee4eb31): standard
    resilience handler on those two clients; webhook and import calls stay single attempts (documented in ADR 0003 and
    the architecture overview).
  - C4 "partial data-retention evidence: no expiry limit, no scheduled purge" — **valid**: expired share links (with
    their password hashes) were never deleted. **Fixed** (ee4eb31): `ShareLinkCleanupJob` purges them every six
    hours; retention documented.
  - D4 ×2 "edited copy of a member" / "duplicated block" between `FeedAddressPolicy` and `CallbackAddressPolicy` —
    **valid as a measurement, deliberately not fixed**: the two members are the trap/plant pair TRP-008/SSRF-002, a
    correct and a bypassable version of the same check. Factoring the IPv4 ranges into one helper would remove the
    range literals from both members and with them the shape that makes the plant and the trap comparable; that would
    tune the repository against the address-classification detector (hiding the trap), which the method forbids.
    Recorded for the coordinator.
  - D1 `FeedAddressPolicy` cyclomatic 17 (threshold 15) — **opinion-not-fact**: the count is right, but it is one flat
    disjunction of eight independent range tests with no nesting; "name the conditions" adds nothing a reader needs.
    Part of the same trap; kept.
  - D8 ×26 "low coverage" per file — **valid**: adapters, controllers and process wrappers had no tests. **Fixed where
    possible without a network** (08e1c45): conversions run against stand-in shell scripts, the guarded connect
    handler against a loopback listener, the EF stores against in-memory SQLite, every controller through the
    in-memory host with stubbed outbound handlers (120 tests now). Adapters that only run against live PostgreSQL
    (Dapper/Npgsql), LDAP or SMTP stay untested: the benchmark's rule is that no test touches a network.
- **False negatives (10), every plant re-verified as real and correctly placed:** SQL-001 (Dapper, interpolated
  literal), SQL-002 (EF `FromSqlRaw` with a pre-built string), CMD-001 (`sh -c` with the format), SSRF-001 (URL from
  `[FromQuery]` binding), DESER-001 (Newtonsoft `TypeNameHandling.All`), XSS-001 (reflected query in `text/html`),
  REDIR-001 (`Redirect(returnUrl)`), REDOS-001 (caller pattern, no timeout), HASH-001 (MD5 password, `MD5.HashData`;
  see the S1 row above), PII-001 (address passed to a source-generated `LoggerMessage`). None of the plants changed.

## 2026-10-07 — scan iteration 2 (final) and freeze

- Contained pass at b355223 (same engine). 32 results; harness (contract 1.1, mapping with message discriminators):
  10 TP, 10 FN, 4 traps caught (TRP-008, TRP-010, TRP-020, TRP-023), 0 clean-region FP, 14 uncovered. Score bands
  unchanged and in band: C2 100, C1 30, S1 40.
- Gone after the iteration-1 fixes: X6 (regex title scrape), X7 (silent "0 pages"), P7 (no resilience), 17 of the 26
  D8 coverage rows.
- Still present, already judged in iteration 1 (code unchanged at those sites): D1 and D4 ×2 on the address-policy
  trap/plant pair (opinion-not-fact / valid-kept), D17 now on trap TRP-023, S1 password-KDF row (the HASH-001 plant at
  `ShareLink.cs:4`), C1 (the BND-002 posture), the three trap hits.
- Remaining D8 rows (9): `ShareStore`, `DocumentSearchRepository`, `SavedSearchStore`, `DocumentRepository` (Dapper /
  Npgsql against PostgreSQL), `ScheduleRepository` (EF with a `DateTimeOffset` ORDER BY that SQLite cannot run),
  `LdapSearcher`, `PeopleEntries` (`SearchResultEntry` has no public constructor), `SmtpMailTransport` — **valid,
  not fixable under the benchmark's no-network rule**; recorded.
- C4 changed from "no expiry limit, no scheduled purge" to "missing: an expiry limit — a declared maximum age / TTL".
  **False-positive**: share links carry a declared maximum age (`CreateShareRequest.ValidDays`, `[Range(1, 30)]`,
  stored as `expires_at`) and `ShareLinkCleanupJob` deletes them after it; subscriber rows live exactly as long as the
  subscription (documented in `docs/architecture.md`). The detector looks for TTL-shaped identifiers; adding one to
  silence it would be appeasement. Code kept.
- Converged: every result is an expected hit, a caught trap, or a judged and recorded finding; the key matches the
  code. Frozen as v1.0.0 with this entry.
