# Benchmark: injection and unsafe input handling (TypeScript)

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its
authoring log is [`journal.md`](journal.md). It is the TypeScript counterpart of `bench-csharp-security-injection`.

## Safety note

The code in this repository is **deliberately vulnerable**. It is a measuring instrument, never deployed and never
run against a network: the tests use in-memory fakes, and nothing connects to a database, a document store, a mail
relay or a remote host. Do not copy its data-access, process, XML, YAML, merge, redirect or URL-fetching code.

## Theme

A small Node.js 22 + TypeScript (strict) **document search and export** API for a records archive, on Express 5. It
searches documents in Postgres (node-postgres), keeps report definitions and schedules through knex, keeps report
subscriptions in MongoDB, converts documents with LibreOffice and ImageMagick child processes, serves attachments and
templates from disk, previews imports by URL, pulls partner feeds, imports document metadata as XML and JSON, loads
report layouts and column mappings as YAML, evaluates computed columns and saved-search filters, protects share
links and export bundles with passwords, mails scheduled reports, keeps an export audit log, and ships a small
browser client (`web/`) that previews XML documents before upload.

Every one of those features takes input from a caller. Each defect sits where real teams put it; each trap is the
safe idiom a careless rule mistakes for the defect. The rest of the repository has the same scaffolding as
`bench-ts-baseline-clean` (CI pinned by commit SHA, CodeQL, Dependabot with a cooldown, README, ADRs, architecture
overview, CHANGELOG, `SECURITY.md`, a committed lock file, strict TypeScript, ESLint, `src/` + `tests/`) so that the
only signal is the theme.

## Labels are about truth

A `must-fire` is something a careful human security reviewer would flag in a code review. A `must-not-fire` is a
site a careless rule would flag and that reviewer would not. Neither label was chosen to match any scanner. Where a
reviewer could reasonably argue the other way, the entry is marked *contested* below, with the reasoning that decided
it.

Concepts (from `scanner-benchmark/taxonomy.json`): `sql-injection` (CWE-89), `nosql-injection` (CWE-943),
`command-injection` (CWE-78), `code-injection` (CWE-94), `path-traversal` (CWE-22), `server-side-request-forgery`
(CWE-918), `xml-external-entity` (CWE-611), `insecure-deserialization` (CWE-502), `prototype-pollution` (CWE-1321),
`cross-site-scripting` (CWE-79), `open-redirect` (CWE-601), `regex-denial-of-service` (CWE-1333), `log-injection`
(CWE-117), `insufficient-password-hashing` (CWE-916), `weak-hash-algorithm` (CWE-328), `sensitive-data-in-logs`
(CWE-532), `sensitive-data-in-url` (CWE-598), `sensitive-data-in-browser-storage` (CWE-922), `uncontrolled-recursion`
(CWE-674). There is **one plant per (concept, file)**, so a scanner that reports one finding per rule and file can
still score every plant.

## Plants (`must-fire`)

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| SQL-001 | sql | `src/search/document-search-repository.ts` | Search term interpolated into a quoted `ILIKE` literal of a node-postgres query. |
| SQL-002 | sql | `src/reports/report-repository.ts` | Owner filter interpolated into the text of a knex `raw` statement. |
| NOSQL-001 | nosql | `src/subscriptions/unsubscribe-routes.ts` | Unvalidated JSON body fields used as a MongoDB filter: operator objects match any subscription. |
| CMD-001 | command | `src/conversion/document-converter.ts` | Caller-chosen export format spliced into a command line run through a shell. |
| PATH-001 | path traversal | `src/attachments/attachment-routes.ts` | Route file name joined onto the storage root and read, no containment check. |
| SSRF-001 | SSRF | `src/imports/import-routes.ts` | Import preview fetches any caller-supplied URL and reports status, type and size. |
| XXE-001 | XXE | `src/imports/metadata-importer.ts` | Uploaded XML parsed by libxml with entity substitution on. |
| DESER-001 | deserialization | `src/reports/layout-format.ts` | Uploaded YAML layouts loaded with a schema that turns a tag into a compiled function. |
| CODE-001 | code | `src/reports/formula-evaluator.ts` | Report formulas run in a `vm` context, which is not a sandbox. |
| CODE-002 | code | `src/saved-searches/filter-compiler.ts` | Saved-search filter expression compiled with `new Function`. |
| PROTO-001 | prototype pollution | `src/preferences/deep-merge.ts` | Recursive merge of a request body copies `__proto__` and writes through it. |
| XSS-001 | XSS (reflected) | `src/search/search-routes.ts` | Raw query echoed into an HTML widget the front end inserts into its page. |
| XSS-002 | XSS (DOM, document value) | `web/src/document-preview.ts` | Text read out of a user-supplied XML document interpolated into `innerHTML`. |
| REDIR-001 | open redirect | `src/shares/share-routes.ts` | Redirect to the `next` query parameter as given. |
| REDOS-001 | ReDoS | `src/search/highlighter.ts` | Caller-supplied pattern compiled and run over whole documents. |
| LOG-001 | log injection | `src/exports/export-audit-log.ts` | Caller-chosen file name written into a plain-text audit line. |
| HASH-001 | password hashing | `src/shares/share-password.ts` | Share-link passwords stored as unsalted MD5. |
| PII-001 | personal data in logs | `src/subscriptions/report-mailer.ts` | Subscriber e-mail address logged on every delivery. |
| PII-002 | personal data in URL | `src/subscriptions/crm-client.ts` | Subscriber e-mail address in the query string of a GET to the CRM. |
| PII-003 | personal data in browser storage | `web/src/recent-recipients.ts` | Recipient e-mail addresses kept in `localStorage`. |
| REC-001 | uncapped recursion | `src/imports/metadata-flattener.ts` | Exported walk over an uploaded JSON document recurses with no depth limit. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | sql | `src/documents/document-repository.ts` | `$1` placeholder, value in the values array. |
| TRP-002 | sql | `src/search/sort-order.ts` | Interpolated `ORDER BY`, both holes from code (lookup table keyed by an enum, two literals). |
| TRP-003 | sql | `src/reports/schedule-repository.ts` | knex `raw` with `?` placeholders and bindings. |
| TRP-004 | nosql | `src/subscriptions/subscription-routes.ts` | MongoDB lookup by a value the schema proved is a string. |
| TRP-005 | command | `src/conversion/thumbnail-renderer.ts` | `execFile`, configured binary, no shell, one argument per value. |
| TRP-006 | path traversal | `src/templates/template-store.ts` | `path.resolve` + `startsWith(root + sep)` before reading. |
| TRP-007 | SSRF | `src/feeds/partner-feed-client.ts` | Request-supplied URL fetched only if https and its host is on the partner allowlist. |
| TRP-008 | XXE | `src/imports/retention-schedule-reader.ts` | libxml with its defaults (no entity substitution, no network). |
| TRP-009 | XXE | `src/feeds/partner-feed-client.ts` | fast-xml-parser, which resolves no external entities. |
| TRP-010 | deserialization | `src/reports/column-mapping.ts` | YAML default schema: plain data only, then schema-validated. |
| TRP-011 | code | `src/reports/row-comparator.ts` | `new Function` whose source is assembled only from a fixed table and two literals. |
| TRP-012 | prototype pollution | `src/reports/layout-overrides.ts` | Merge into `Object.create(null)`, dangerous keys refused, known keys only. |
| TRP-013 | XSS | `src/documents/card-renderer.ts` | Template engine with auto-escaping on. |
| TRP-014 | XSS (DOM) | `web/src/document-list.ts` | Values escaped before they enter the `innerHTML` template. |
| TRP-015 | open redirect | `src/saved-searches/saved-search-routes.ts` | Redirect only to a validated same-origin path, else a fixed page. |
| TRP-016 | ReDoS | `src/search/term-matcher.ts` | RegExp built from a term whose metacharacters are all escaped. |
| TRP-017 | ReDoS | `src/documents/document-number.ts` | Constant anchored pattern without nested quantifiers. |
| TRP-018 | weak hash | `src/documents/etag.ts` | MD5 as an HTTP ETag. *Contested*, see below. |
| TRP-019 | password hashing | `src/exports/download-password.ts` | bcrypt, cost 12. |
| TRP-020 | log injection | `src/exports/export-service.ts` | File name logged as a field of a structured JSON record. |
| TRP-021 | personal data in logs | `src/subscriptions/report-mailer.ts` | Bounce logged with a keyed HMAC of the address. *Contested*, see below. |
| TRP-022 | personal data in URL | `src/subscriptions/crm-client.ts` | `email=true` is a channel switch on an opaque id. |
| TRP-023 | browser storage | `web/src/view-settings.ts` | `localStorage` holds page size and theme only. |
| TRP-024 | uncapped recursion | `src/saved-searches/filter-tree.ts` | Recursion that carries its depth and refuses deep trees. |

## Contested truths

- **TRP-018 (MD5 ETag).** Some policies ban MD5 everywhere. A reviewer judging *this* code asks what property the
  hash must provide: an ETag only has to change when the bytes change; a collision would at worst serve a stale cached
  card of a document the caller may already read. Not a defect.
- **TRP-021 (pseudonymised log).** A keyed HMAC of an e-mail address is still personal data under GDPR (pseudonymised,
  not anonymised), but it is exactly the "stable pseudonymous reference" that data-minimisation guidance recommends
  logging in place of the address, and it cannot be reversed without the key. Not a defect.
- **REC-001 (recursion in JavaScript).** Unlike .NET, exhausting the stack in Node.js throws a catchable
  `RangeError`, so the process survives. The walk is still a defect: the depth of the call stack is chosen by the
  document's author, the failure is an unhandled 500 for a well-formed input, and the same function is reachable from
  code that does not expect it to throw. Depth-unbounded recursion over parsed input is reported as CWE-674 across the
  npm ecosystem.
- **HASH-001 concept.** Unsalted MD5 of a password is both a weak hash and an inadequate password hash; the entry
  names the more precise concept (`insufficient-password-hashing`). The taxonomy treats the two as one family.
- **DESER-001 vs. CODE-*.** The layout loader compiles a function, so a rule about evaluated strings may also report
  it; what makes it a defect is that an uploaded *data* format can carry code, which is CWE-502.
- **SSRF-001 vs. ADR 0003.** The ADR gives imports by URL a timeout and a size cap; neither limits *where* the server
  connects, which is the defect.
- **XSS-001 and the CSP header.** The API sends `Content-Security-Policy: default-src 'none'` on its own responses,
  but the widget is fetched by the archive front end and inserted into *its* page, where that header does not apply.

## Certified clean

Every other tracked file (lock file aside) is certified free of the concepts above plus the remaining
injection-family and unsafe-input concepts (`ldap-injection`, `xpath-injection`, `weak-cryptographic-algorithm`,
`insecure-randomness`, `missing-authorization`, `mass-assignment`, `error-information-exposure`,
`improper-certificate-validation`, `token-signature-or-expiry-not-validated`). Clean entries certify those concepts,
not every kind of defect. The two unauthenticated endpoints (unsubscribe and share unlock) are public by design: each
is addressed by an unguessable token.

## Not applicable

- `ldap-injection` (`NA-001`): no directory access.
- `xpath-injection` (`NA-002`): XML is read by walking the parsed tree, never by expression.
- `unbounded-allocation-from-untrusted-length` (`NA-003`): no binary format with a length field is read.

## Score bands

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | inbound-input-validation | 30–70 | Schemas everywhere but shape-only; two routes skip validation; headers and transport good; MD5 passwords and personal data in a URL. Middling. |

## Deliberately not covered

- Hard-coded secrets (`bench-ts-security-secrets`), vulnerable dependencies (`bench-ts-security-dependencies`) and
  CI/IaC rules are measured elsewhere; results of those concepts here are `uncovered`, not noise.
