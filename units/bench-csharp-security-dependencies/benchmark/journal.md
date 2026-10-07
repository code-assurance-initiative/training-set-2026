# Authoring journal — bench-csharp-security-dependencies

## 2026-10-07 — answer key v1.0.0 drafted (key first)

- Wrote `benchmark/answer-key.json` before any code: 8 `must-fire` (DEP-001…008), 6 `must-not-fire` (TRP-001…006),
  2 `score-band`, and `clean` entries for the planned tree. Lines are planned positions; they are resolved from the
  final files in step 2.
- Every package fact was checked on 2026-10-07: advisories with `POST https://api.osv.dev/v1/query` (and
  `/v1/vulns/<id>` for the fixed versions), deprecation and licence expressions from the nuget.org registration and
  flat-container nuspec, .NET support dates from Microsoft's `releases-index.json`. Before writing the key the
  package set was restored and built in a scratch project (net10.0 and net6.0) to make sure it restores with the
  10.0 SDK and produces no advisory beyond the planted ones.
- Decisions (see `benchmark/README.md`, "Contested truths"): the test-only vulnerable package is a plant, not a trap;
  licences are judged against the product's written policy (proprietary shipped bundle; LGPL allowed unmodified);
  outdated rows on the vulnerable plants are redundant, not separate plants; malicious dependency gets a trap and
  clean regions but no plant (a malicious NuGet package cannot be restored); Dependabot is deliberately absent.
- Candidates rejected while choosing the package set, so the plants stay single-signal: `itext7` (AGPL, but every
  version is also deprecated), `Spectre.Console` in the net6.0 exporter (its current release drags in a package that
  does not support net6.0), `Markdig` as the 0.x look-alike (it reached 1.0 in 2026), a `net9.0` end-of-life trap or
  plant (.NET 9 support ends 2026-11-10 — true today, false in a month; a frozen key cannot carry it).
- Pending: the `Cronos` 0.13.0 "looks like a pre-release, is stable" trap needs a `prerelease-dependency` concept in
  the harness taxonomy; it is described in the README and added to the key once the concept exists.
- Validated with `python3 -m cai_bench validate` against `taxonomy.json`: OK.

## 2026-10-07 — implemented; key lines fixed to the code

- Implemented the product: `Invoicing.Contracts` (netstandard2.0;net10.0), `Invoicing.Rendering` (PdfSharpCore,
  UBL 2.1, XML-DSig), `Invoicing.Api` (controllers, JWT scope policies, ERP webhook on Newtonsoft.Json, branding
  client with a Polly retry handler), `Invoicing.Worker` (MySQL render queue, MailKit, Cronos schedule),
  `tools/Invoicing.ArchiveExporter` (net6.0; SharpZipLib, CsvHelper, iTextSharp.LGPLv2.Core), unit and
  integration tests (xUnit v3, FluentAssertions 7, Bogus, SharpCompress). `dotnet build -c Release`: 0 warnings
  (advisory warnings NU1901–1904 are reported, not errors, by `Directory.Build.props`); `dotnet test`: 22 + 12
  passed, repeated eight times to rule out order dependence (an early version shared one seeded Bogus generator
  across parallel tests and was flaky; each invoice is now seeded from its number).
- `dotnet list package --vulnerable --include-transitive`, `--deprecated` and `--outdated` show exactly the planted
  signals: Newtonsoft.Json, SixLabors.ImageSharp (transitive), SharpCompress, SharpZipLib vulnerable;
  Polly.Extensions.Http deprecated; Newtonsoft.Json, SharpCompress, SharpZipLib, CsvHelper and FluentAssertions
  outdated (the first three are the vulnerable plants — redundant by decision; FluentAssertions is TRP-004).
- Found while implementing: CsvHelper 12's `Configuration.CultureInfo` does not reset the delimiter, so on a Danish
  host the index came out semicolon-separated; the exporter sets the delimiter explicitly (fixed before commit).
- Key changes against the draft, each because the code made the site more precise or the harness grew:
  - TRP-007 added (`Cronos` 0.13.0, stable 0.x look-alike). The concept `prerelease-dependency` did not exist; added to
    the harness taxonomy and the reference mapping (scanner-benchmark 4c0f4a7: concepts.py, dims.py, discrim.py,
    regenerated files, and the one mapping test whose fixture expected "Prerelease dependency:" rows to map to no
    concept).
  - DEP-002 rationale corrected: the exporter only writes archives, so the advisories' extraction path is not reached;
    the version is still affected.
  - DEP-003 spans the whole lock-file entry (5 lines), so a result on its `resolved` line matches.
  - `lineTolerance` set to 0 (README, "Line tolerance is 0").
  - The ADR set became 0001 PdfSharpCore, 0002 render queue in the billing database, 0003 licence policy, 0004
    FluentAssertions 7 (the draft's "exporter stays on net6" ADR was dropped: a record that accepts the end-of-life
    runtime would not change the fact, and the code says what it is).
  - Clean entries regenerated for every tracked file (96), lock files restricted to the concepts no plant or trap can
    surface in them.
- All `lines` resolved from the committed tree by pattern and checked with `sed -n`; the key validates.

## 2026-10-07 — scan iteration 1

- Scanner: the reference scanner at engine commit 6a05dfb6c, rubric-2026.10.1, contained mode, over repository commit
  9d0089f; 21 results; `report.sarif` sha256 d749a65f…c003c.
- **Every dependency result is repository-level (no file, no line).** The harness can only match a located entry with
  a located result, so its table reads 0 % recall and 100 % noise for the dependency concepts. That is a harness
  limitation for scanners that report per package rather than per line, not a property of the key (the key places
  each fact on the line a reviewer would edit). The outcomes below are judged from the raw results, package by package.
- Plants found (judged): DEP-001 Newtonsoft.Json (D12 "Vulnerable" + D30, one logical finding), DEP-002 SharpZipLib
  (D12 + D30), DEP-003 SixLabors.ImageSharp transitive (D30 only — the D12 arm lists direct packages), DEP-004
  SharpCompress test-only (D12 + D30), DEP-005 Polly.Extensions.Http deprecated (D12), DEP-007 MySql.Data GPL (D14),
  DEP-008 net6.0 (D44). **Missed: DEP-006 CsvHelper outdated** — the scanner counted it (`outdated_count: 5`, and
  `findings.md` lists "CsvHelper 12.1.2 → 33.1.0") but emits outdated rows at Info level, and no Info row reaches the
  SARIF report; a false negative at the reporting layer.
- Traps: **TRP-003 caught** — iTextSharp.LGPLv2.Core reported as "Banned license" because the banned set is matched by
  substring and `GPL-2.0` is a substring of `LGPL-2.0-only` (false-positive; the policy allows LGPL for an unmodified
  separate assembly). TRP-001/002 (patched versions), TRP-004 (FluentAssertions 7 — trivially, since no outdated row is
  emitted at all), TRP-005 (Bogus), TRP-006 (netstandard2.0 multi-target) and TRP-007 (Cronos 0.x): left alone.
- Off-theme results, verdicts:
  - D8 low coverage on `FileSigningCertificateSource`, `BrandingServiceClient`, `InvoicePayload`,
    `MySqlRenderJobStore` (0 %): **valid** — fixed by tests for the first three and for the store's failure path; the
    store's SQL still needs a MySQL server, which the offline test rule excludes (recorded; expected to stay low).
  - X5 four null-forgiving operators (`XmlInvoiceSigner`, `UblInvoiceWriter`): **valid** — removed.
  - X18 `PdfStamper` not disposed on the exception path: **valid** — now in a `using` block.
  - D17 `.editorconfig:37` CA2007 off "with no record why": **false-positive** — the reason is on line 36 and
    `src/`/`tools/.editorconfig` re-enable the rule; promoted to TRP-008 (as baseline TRP-001). Comment on line 36 now
    also names `tools/`.
  - D29 `XmlInvoiceSigner.cs:56` "XmlDocument loaded with DTD processing": **false-positive** — line 56 is
    `SignedXml.LoadXml(XmlElement)`, which parses nothing; the signer is only ever handed documents this code built.
  - D29 `ArchiveExporter.cs:32` unsafe `Path.Combine`: **false-positive** — the directory is the operator's own
    command-line argument and the file name is an invoice number validated to `[A-Z0-9-]{1,32}` two lines earlier.
  - D5 `Invoicing.Contracts` off the main sequence: **opinion-not-fact** — the message itself says a shared model
    library has this shape by design.
- Score band BND-001 (`dependencies-not-locked`) shows "out": the score adapter supplies D12's score (37, dominated by
  the planted vulnerabilities) for the concept, while the dimension that measures locking (SC1) is advisory and
  publishes no score. Not a property of the repository; recorded for the coordinator.
- Key change: TRP-008 added (above). Repository fixes: commit "fix: no null-forgiving operators …".

## 2026-10-07 — scan iteration 2 (converged) and freeze

- Same scanner and mode, over repository commit 99a9821; 16 results; `report.sarif` sha256 3d9b5cc3…1244.
- Plants and traps: identical to iteration 1 (seven of eight plants found by the raw results, DEP-006 missed at the
  reporting layer; TRP-003 caught, the other seven traps left alone, TRP-008 caught again as expected).
- The three repository fixes held: the X5 and X18 rows are gone, and the D8 rows on the branding client, the
  certificate source and the stored payload are gone. `MySqlRenderJobStore` remains at 21 % line coverage — **valid,
  residual by construction**: the rest is SQL that needs a MySQL server, and this repository's tests must run offline.
- Every other result is recorded noise from iteration 1, at unchanged sites (the D29 DTD row moved from line 56 to 58
  with the null-forgiving fix; same call, same verdict).
- Converged: the scan's unexpected results are recorded noise plus the documented coverage residual, and the key
  matches the code. Frozen as v1.0.0.

## 2026-10-07 — v1.1.0: entries name their package (contract 1.2)

- Every result of the dependency, licence, vulnerability and end-of-life checks is location-less (the package, not a
  line, is the site), so under contract 1.1 no located entry could ever be matched and the harness reported 0 %
  recall for 7 real detections. Harness contract 1.2 adds `subject` (whole-token match against the message).
- **Key change:** every must-fire and must-not-fire now carries `subject` — the NuGet id from its rationale, or the
  target framework (`net6.0`, `netstandard2.0`). Nothing else changed: same entries, lines, labels and code.
  v1.0.0 stays as frozen; this is a new version (`v1.1.0`), as the plan requires for any change after freeze.
- Re-scored scan iteration 2 with this key: recall 7/8, trap resistance 6/8, noise 2/12 — the outcomes the judge
  recorded by hand in iteration 2 (FN DEP-006; trap catches TRP-003 and TRP-008).
