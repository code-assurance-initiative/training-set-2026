# Benchmark: injection and unsafe input handling

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its
authoring log is [`journal.md`](journal.md).

## Safety note

The code in this repository is **deliberately vulnerable**. It is a measuring instrument, never deployed and never
run against a network: the tests use in-memory fakes, and nothing connects to a database, a directory, a mail relay
or a remote host. Do not copy its data-access, process, XML, deserialization, redirect or address-check code.

## Theme

A small ASP.NET Core (`net10.0`) **report builder and document search** API for a document archive. It searches
documents in Postgres (Dapper and `NpgsqlCommand`), keeps report definitions and schedules in EF Core, converts
documents with LibreOffice and ImageMagick child processes, serves attachments and report templates from disk,
imports documents by URL and from an uploaded bundle format, pulls a partner feed, calls tenant webhooks, imports
document metadata as XML, deserializes saved-search files and report layouts, renders HTML snippets for the archive
front end, looks up owners and groups in LDAP, highlights search hits with regular expressions, protects share links
with a password, and mails scheduled reports.

Every one of those features takes input from a caller. Each defect sits where real teams put it; each trap is the
safe idiom a careless rule mistakes for the defect. The rest of the repository has the same scaffolding as
`bench-csharp-baseline-clean` (CI pinned by commit SHA, CodeQL, Dependabot, README, ADRs, architecture doc,
CHANGELOG, `SECURITY.md`, central package management with lock files, nullable, `src/` + `tests/`) so that the only
signal is the theme.

## Labels are about truth

A `must-fire` is something a careful human security reviewer would flag in a code review. A `must-not-fire` is a
site a careless rule would flag and that reviewer would not. Neither label was chosen to match any scanner. Where a
reviewer could reasonably argue the other way, the entry is marked *contested* below, with the reasoning that decided
it.

Concepts (from `scanner-benchmark/taxonomy.json`): `sql-injection` (CWE-89), `command-injection` (CWE-78),
`path-traversal` (CWE-22), `server-side-request-forgery` (CWE-918), `xml-external-entity` (CWE-611),
`xpath-injection` (CWE-643), `insecure-deserialization` (CWE-502), `cross-site-scripting` (CWE-79), `open-redirect`
(CWE-601), `ldap-injection` (CWE-90), `regex-denial-of-service` (CWE-1333), `weak-hash-algorithm` (CWE-328),
`sensitive-data-in-logs` (CWE-532), `sensitive-data-in-url` (CWE-598), `unbounded-allocation-from-untrusted-length`
(CWE-789), `uncontrolled-recursion` (CWE-674). There is **one plant per (concept, file)**, so a scanner that reports
one finding per rule and file can still score every plant.

## Plants (`must-fire`)

`API` = `src/ReportDesk.Api`, `C` = `src/ReportDesk.Api/Controllers`.

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| SQL-001 | sql | `API/Search/DocumentSearchRepository.cs` | Search term interpolated into a quoted `ILIKE` literal of a Dapper query. |
| SQL-002 | sql | `API/Reports/ReportRepository.cs` | Owner filter interpolated into a string later passed to EF Core `FromSqlRaw`. |
| SQL-003 | sql | `API/SavedSearches/SavedSearchStore.cs` | Saved-search name concatenated into an `NpgsqlCommand.CommandText` `DELETE`. |
| CMD-001 | command | `API/Conversion/DocumentConverter.cs` | Caller-chosen export format spliced into a `/bin/sh -c` command line. |
| PATH-001 | path traversal | `API/Attachments/AttachmentStore.cs` | Attachment file name from the route `Path.Combine`d onto the root and opened, no containment check. |
| SSRF-001 | SSRF | `C/ImportsController.cs` | Import preview GETs any caller-supplied URL from inside the network and returns its status, type and size. |
| SSRF-002 | SSRF (address check) | `API/Webhooks/CallbackAddressPolicy.cs` | Public-address check unwraps IPv4-mapped IPv6 only; NAT64, 6to4 and IPv4-compatible forms of internal hosts are classified public. |
| XXE-001 | XXE | `API/Metadata/MetadataImporter.cs` | Uploaded XML loaded into `XmlDocument` with `XmlResolver = new XmlUrlResolver()`. |
| XPATH-001 | XPath | `API/Metadata/MetadataFieldReader.cs` | Field name from the route concatenated into the XPath predicate that hides restricted fields. |
| DESER-001 | deserialization | `API/SavedSearches/SavedSearchSerializer.cs` | Uploaded saved-search files read with Newtonsoft `TypeNameHandling.All` into a model with `object` values. |
| XSS-001 | XSS (reflected) | `C/SearchWidgetController.cs` | Raw query echoed into a `text/html` widget the front end inserts into its page. |
| XSS-002 | XSS (document value) | `API/Previews/MetadataPreviewRenderer.cs` | `XmlNode.InnerText` from the uploaded document written into a quoted attribute and element text unencoded. |
| REDIR-001 | open redirect | `C/ReportsController.cs` | `Redirect(returnUrl)` with no check. |
| LDAP-001 | LDAP | `API/People/OwnerDirectory.cs` | E-mail address concatenated into an LDAP filter without RFC 4515 escaping. |
| REDOS-001 | ReDoS | `API/Search/HighlightService.cs` | Caller-supplied pattern compiled with no match timeout and run over whole documents. |
| HASH-001 | weak hash | `API/Shares/SharePasswordHasher.cs` | Share-link passwords stored as unsalted MD5 (`MD5.HashData`). |
| PII-001 | personal data in logs | `API/Delivery/ReportMailer.cs` | Subscriber e-mail address logged on every delivery (source-generated `LoggerMessage`; the call site is keyed). |
| PII-002 | personal data in URL | `API/Delivery/SubscriberDirectoryClient.cs` | Subscriber e-mail address in the query string of a GET to the CRM. |
| LEN-001 | untrusted length | `API/Importing/BundleReader.cs` | Entry length read from the uploaded bundle (`BinaryReader.ReadInt32`) spent on `ReadBytes` unbounded. |
| REC-001 | uncapped recursion | `API/Metadata/MetadataFlattener.cs` | Public `Flatten(XElement)` recurses into every child of the uploaded document with no depth limit. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | sql | `API/Documents/DocumentRepository.cs` | Dapper `WHERE id = @Id` with the value bound from an anonymous object. |
| TRP-002 | sql | `API/Documents/DocumentRepository.cs` | Interpolated `ORDER BY {column} {direction}`, both from code: a fixed column dictionary and two literals. |
| TRP-003 | sql | `API/Reports/ScheduleRepository.cs` | EF Core `FromSqlInterpolated`: every hole becomes a parameter. |
| TRP-004 | sql | `API/SavedSearches/SavedSearchStore.cs` | Constant `CommandText`, values through `Parameters.AddWithValue`. |
| TRP-005 | command | `API/Conversion/ThumbnailRenderer.cs` | Configured executable, no shell, every value its own `ArgumentList` element; size from an enum, paths generated by the service. |
| TRP-006 | path traversal | `API/Templates/TemplateStore.cs` | `Path.GetFullPath` + ordinal `StartsWith(root + separator)` before opening. |
| TRP-007 | SSRF | `API/Feeds/PartnerFeedClient.cs` | Request-supplied feed URL fetched only if https and its host is on the configured allowlist. |
| TRP-008 | SSRF (address check) | `API/Feeds/FeedAddressPolicy.cs` | Unwraps IPv4-mapped IPv6 like SSRF-002, but refuses every other IPv6 address, so no embedding passes. |
| TRP-009 | XXE | `API/Metadata/RetentionScheduleReader.cs` | `XmlReader` with `DtdProcessing.Prohibit` and no resolver. |
| TRP-010 | XPath | `API/Metadata/MetadataSectionReader.cs` | Concatenated XPath, but the value passed `XmlConvert.VerifyNCName` first. |
| TRP-011 | deserialization | `API/Reports/ReportLayoutSerializer.cs` | Newtonsoft with `TypeNameHandling.None` set explicitly. |
| TRP-012 | XSS | `C/SearchWidgetController.cs` | Document titles written through `HtmlEncoder.Default.Encode`. |
| TRP-013 | XSS (document value) | `API/Previews/DocumentCardRenderer.cs` | Value from the document XML HTML-encoded before it enters an attribute. |
| TRP-014 | XSS (document value) | `API/Previews/DocumentCardRenderer.cs` | Value from the document XML parsed to `int` before it enters an attribute. |
| TRP-015 | open redirect | `C/SharesController.cs` | `Url.IsLocalUrl(returnUrl)` then `LocalRedirect`, else a fixed page. |
| TRP-016 | LDAP | `API/People/GroupDirectory.cs` | Concatenated filter, value through an RFC 4515 escaper. |
| TRP-017 | ReDoS | `API/Documents/DocumentNumber.cs` | Constant anchored pattern, no nested quantifiers, 100 ms timeout. |
| TRP-018 | weak hash | `API/Http/ETagCalculator.cs` | MD5 as an HTTP ETag for cache validation; no security property depends on it. *Contested*, see below. |
| TRP-019 | personal data in logs | `API/Delivery/ReportMailer.cs` | Bounce logged with a keyed HMAC of the address (`emailAddressHash`), not the address. *Contested*, see below. |
| TRP-020 | personal data in URL | `API/Delivery/SubscriberDirectoryClient.cs` | `email=true` is a channel switch on an opaque subscription id. |
| TRP-021 | untrusted length | `API/Importing/BundleIndexReader.cs` | Name length from the file checked `0 < n <= 1024` before `ReadBytes`. |
| TRP-022 | uncapped recursion | `API/Search/FilterTreeCompiler.cs` | Recursion over a `JsonNode` tree with no counter of its own, but System.Text.Json refuses input nested deeper than 64. *Contested*, see below. |
| TRP-023 | suppressed diagnostic | `.editorconfig` | CA2007 switched off at the root with its reason on the line above; `src/.editorconfig` turns it back on for production code. Off-theme; promoted from a scanner false positive (iteration 1), as in `bench-csharp-baseline-clean`. |

## Contested truths

- **TRP-018 (MD5 ETag).** Some policies ban MD5 everywhere. A reviewer judging *this* code asks what property the
  hash must provide: an ETag only has to change when the bytes change; a collision would at worst serve a stale cached
  preview of a document the caller may already read. Not a defect.
- **TRP-019 (pseudonymised log).** A keyed HMAC of an e-mail address is still personal data under GDPR (pseudonymised,
  not anonymised), but it is exactly the "stable pseudonymous reference" that data-minimisation guidance recommends
  logging in place of the address, and it cannot be reversed without the key. Not a defect.
- **TRP-022 (JsonNode recursion).** The walker has no depth parameter, which a structural rule notices. Its input can
  only be a `JsonNode` produced by System.Text.Json, whose reader enforces `MaxDepth` (64 by default, and the
  application does not raise it). A recursion bounded at 64 frames cannot exhaust the stack. Contrast REC-001: XML
  parsing has no depth limit, so the same shape over `XElement` is a defect.
- **SSRF-001 vs. ADR 0003.** The ADR gives imports by URL a timeout and a size cap; neither limits *where* the server
  connects, which is the defect.
- **XSS-001 and the CSP header.** The API sends `Content-Security-Policy: default-src 'none'` on its own responses,
  but the widgets are fetched by the archive front end and inserted into *its* page, where that header does not
  apply. The reflection is a defect at the API.

## Certified clean

Every other file under `src/` and `tests/` (lock files aside), both workflows and the two `Directory.*.props` files
(`CLN-*`) are certified free of the sixteen concepts
above plus the remaining injection-family and unsafe-input concepts (`code-injection`, `weak-cryptographic-algorithm`,
`insecure-randomness`, `missing-authorization`, `mass-assignment`, `error-information-exposure`,
`improper-certificate-validation`, `token-signature-or-expiry-not-validated`). Clean entries certify those concepts,
not every kind of defect.

One file is deliberately **neither labelled nor certified**: `API/Webhooks/WebhookDispatcher.cs` posts to a
tenant-chosen callback, and whether that is safe depends entirely on `CallbackAddressPolicy` (SSRF-002). A result
there is judged in the journal rather than pre-scored.

The API project sets `<WarningsNotAsErrors>CA5351</WarningsNotAsErrors>`: the compiler's broken-crypto analyzer still
reports both MD5 sites (HASH-001 and the TRP-018 ETag) as warnings on every build, but does not fail it. This keeps
the build green without suppressing a diagnostic at a planted site.

## Not applicable

- `code-injection` (`NA-001`): nothing is evaluated as code; there is no template engine.

## Score bands

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | authorization-enforcement | 80–100 | Scope policies, fallback policy, `[Authorize]` on every controller. |
| BND-002 | data-encryption-controls | 10–50 | Personal data held; HTTPS enforced, but nothing encrypted at rest and passwords in MD5. |
| BND-003 | inbound-input-validation | 30–70 | Models validated for presence and length only; the dangerous fields are unconstrained; web posture otherwise good. |

## Deliberately not covered

- **Log forging** (CWE-117): the taxonomy has no concept for it, and a concatenated log message is measured as
  non-structured logging by `bench-csharp-codehealth`. Every log call here uses a structured template.
- Hard-coded secrets (`bench-csharp-security-secrets`), vulnerable dependencies (`bench-csharp-security-dependencies`)
  and CI/IaC rules (`bench-csharp-security-iac`) are measured elsewhere; results of those concepts here are
  `uncovered`, not noise.
