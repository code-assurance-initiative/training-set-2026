# bench-csharp-tests — test-suite quality

Part of the [code-assurance-initiative scanner benchmark](https://github.com/code-assurance-initiative/scanner-benchmark).
Labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); the authoring log is
[`journal.md`](journal.md).

## Theme

A small .NET (net10.0) **currency conversion & rounding** library with an ASP.NET Core API host: ISO 4217 currencies,
`Money`, rounding modes, allocation without losing minor units, exchange-rate tables from the European Central Bank
feed, a rate cache, cross-rate conversion and quotes with fees. The production code is written to be clean; the
benchmark is about its **tests**: whether they assert, whether they can fail, whether they are deterministic, whether
they test behaviour or wiring, whether the CI gate guards what it measures, and whether test code leaks into
production.

The suite is green: every defect here is a *quality* defect a careful reviewer would flag, not a red test.

## Plants (`must-fire`)

| Id | Concept | Site | Why it is a defect |
|---|---|---|---|
| TQ-001 | test-without-assertion | `UnitTests/Rates/CurrencyConverterTests.cs` | Converts a large amount and never checks the result — passes unless something throws |
| TQ-002 | test-without-assertion | `UnitTests/Allocation/AllocatorTests.cs` | Ends in `Assert.True(true)`: an assertion that is true by construction verifies nothing |
| TQ-003 | skipped-test-without-reason | `UnitTests/Rates/CurrencyConverterTests.cs` | `Skip = "wip"` — no reason, no category, no issue |
| TQ-004 | flaky-test | `UnitTests/Rates/RateRefreshServiceTests.cs` | Starts background work with `Task.Run`, then a fixed `Thread.Sleep` before asserting |
| TQ-005 | flaky-test | `UnitTests/Rates/EcbRateSourceTests.cs` | Fetches the live ECB feed from the unit suite (skips itself when offline) |
| TQ-006 | flaky-test | `UnitTests/Rates/RateTableTests.cs` | Expectation from `DateTime.Today` (local) against code using the UTC date — fails while local and UTC dates differ |
| TQ-007 | excessive-mocking | `UnitTests/Quotes/QuoteServiceTests.cs` | Six substitutes, asserts only `Received()` calls — tests wiring, not the quote |
| TQ-008 | test-failure-swallowed | `UnitTests/Monetary/MoneyFormatterTests.cs` | Act and `Assert` inside `catch (Exception)` that only logs — the test cannot fail |
| COV-001 | test-coverage | `src/Fx.Conversion.Core/Rounding/CashRounding.cs` | Core cash-rounding module with no test at all (whole file) |
| AX8-001 | production-depends-on-test-code | `src/Fx.Conversion.Api/Fx.Conversion.Api.csproj` | The API references the test-support project (and through it the xUnit assertion library) |
| GATE-001 | ci-test-gate-integrity | repository (`.github/workflows/ci.yml`) | Coverage collected and uploaded, never gated |

### How flaky is the flaky test (TQ-006)?

`RateTable.AgeInDays` counts days from the publication date to the **UTC** date of `TimeProvider.System`; the test
builds the table for `DateOnly.FromDateTime(DateTime.Today)`, the **local** date, and expects age 0. On a UTC machine (every hosted CI
runner) the two always agree and the test always passes. On a developer machine in Copenhagen it fails between 00:00
and 01:00 (winter) or 02:00 (summer) local time; in New York between 19:00/20:00 and midnight. Real and reproducible
(`TZ=Pacific/Pago_Pago dotnet test` fails it before 11:00 UTC), and rare enough that the suite is green almost every
time it is run on a developer machine. A re-run-based detector will normally not observe it; a reader of the code can.

## Traps (`must-not-fire`)

| Id | Concept | Site | Why it is NOT a defect |
|---|---|---|---|
| TRP-001 | test-without-assertion | `UnitTests/Monetary/MoneyTests.cs` | Asserts through Shouldly (`ShouldBe`) |
| TRP-002 | test-without-assertion | `UnitTests/Monetary/MoneyTests.cs` | `Assert.Throws<…>` is the whole test, and it is an assertion |
| TRP-003 | skipped-test-without-reason | `UnitTests/Allocation/AllocatorTests.cs` | Skip with a categorised reason (`BUG:`) naming a real, reproducible bug and linking its issue (#1) |
| TRP-004 | flaky-test | `UnitTests/Rates/RateCacheTests.cs` | Expiry tested on a `FakeTimeProvider` — virtual time |
| TRP-005 | flaky-test | `IntegrationTests/RatesEndpointTests.cs` | `WebApplicationFactory` in-memory server on the reserved `.test` domain — no live host |
| TRP-006 | flaky-test | `UnitTests/Rates/EcbRateSourceTests.cs` | Real ECB URL, but the `HttpClient` runs on the test's stub handler and a checked-in fixture |
| TRP-007 | excessive-mocking | `UnitTests/Rates/CurrencyConverterTests.cs` | One substitute, for the external rate source; asserts the converted amount |
| TRP-008 | flaky-test | `UnitTests/Rates/EcbRateSourceTests.cs` | Polly retry on a `FakeTimeProvider`; only a `WaitAsync` hang guard, no sleep |
| TRP-009 | test-without-assertion | `UnitTests/Allocation/AllocatorTests.cs` | CsCheck property test: `Sample(predicate)` throws with a shrunk counter-example |
| TRP-010 | flaky-test | `UnitTests/Rates/RateRefreshServiceTests.cs` | Waits for the cache by polling the condition up to a deadline — the remedy, not the defect |
| TRP-011 | production-depends-on-test-code | `src/Fx.Conversion.Core/Fx.Conversion.Core.csproj` | `InternalsVisibleTo` the unit-test assembly is not a dependency on it |
| TRP-012 | flaky-test | `UnitTests/Monetary/MoneyFormatterTests.cs` | Culture-specific output with the culture passed explicitly |
| TRP-013 | test-failure-swallowed | `UnitTests/Currencies/CurrencyCatalogTests.cs` | `catch (Exception)` that calls `Assert.Fail` naming the case — the failure still reaches the runner |

## What is certified clean

`clean` entries cover every other source file for the concepts this repository measures: the five test-quality
concepts (`test-without-assertion`, `skipped-test-without-reason`, `excessive-mocking`, `flaky-test`,
`test-failure-swallowed`) on every C# and feature file without a plant; `test-coverage` on every production C# file
except `CashRounding.cs` (each is exercised by the unit or integration suite); `production-depends-on-test-code` on every
project file except the API's.

## What the repository does not cover

Security, code health, architecture beyond test isolation, domain modelling and operations are out of theme. The
code is written to the shared C# conventions so that it does not trip them, but the key makes no claim about those
concepts (a result for one of them is `uncovered`, not noise).

## Score bands — chosen from intent, before any scan

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | test-coverage | 60–95 | Well covered, with one untested core module |
| BND-002 | test-pyramid-distribution | 70–100 | Unit base, in-memory integration layer, a few BDD scenarios, no e2e |
| BND-003 | domain-vs-controller-coverage | 60–100 | Coverage concentrates on the domain library; controllers are thin |
| BND-004 | executable-specifications | 80–100 | Gherkin features bound to steps, run by the xUnit runner on every build |
| BND-005 | ci-test-gate-integrity | 30–75 | Whole suite runs on PRs, but coverage is not gated and one skip has no reason |

## Reproduce

```bash
git clone https://github.com/code-assurance-initiative/bench-csharp-tests
git clone https://github.com/code-assurance-initiative/scanner-benchmark
cd bench-csharp-tests && dotnet build -c Release && dotnet test -c Release --collect:"XPlat Code Coverage"
cd ../scanner-benchmark
python3 -m cai_bench validate --key ../bench-csharp-tests/benchmark/answer-key.json --taxonomy taxonomy.json
python3 -m cai_bench score --help
```
