# Benchmark: production readiness, with realistic gaps

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`, schema 1.2);
its authoring log is [`journal.md`](journal.md).

## Theme

**ParcelTracking**, a small parcel-tracking service in .NET (`net10.0`) that is *almost* ready for production. It has
what a competent team ships — CI, CodeQL, Dependabot, lock files, nullable analysis, health checks, OpenTelemetry,
versioned EF Core migrations shipped as a bundle, Kubernetes manifests with probes and rolling updates, a deploy
workflow behind an approval gate with automatic rollback, a backup/restore runbook, a SECURITY.md — and a dozen
gaps of the kind that survive review because each one sits next to code that does the same thing right.

| Project | Role |
|---|---|
| `ParcelTracking.Core` | parcels, tracking events, status rules, carrier status normalisation, the tracking service |
| `ParcelTracking.Infrastructure` | EF Core (PostgreSQL) persistence and migrations, the carrier API client, the merchant webhook client, health checks |
| `ParcelTracking.Api` | ASP.NET Core API (JWT bearer, named policies), label download branch, `/.well-known/security.txt` |
| `ParcelTracking.Worker` | background host: polls carriers for active parcels, dispatches merchant webhooks from an outbox table |
| `ParcelTracking.Client` | the published .NET client library merchants use to call the API |
| `ParcelTracking.Cli` | `parcel-admin`, an operator command-line tool that checks carrier status-mapping files |
| `benchmarks/ParcelTracking.Benchmarks` | BenchmarkDotNet over the status-normalisation hot path |

Two upstream HTTP services (the carrier tracking API and merchants' webhook endpoints) and one database.

### Deployment shape: present, and why

A readiness benchmark without a deployment cannot say anything about rollout, rollback or recovery, and the EF Core
migrations-bundle trap needs a pipeline that runs it. So the repository carries digest-pinned Dockerfiles,
hardened Kubernetes manifests (`deploy/k8s`), a tag-triggered release workflow and a manual, environment-gated deploy
workflow. They are written to be clean; deployment and recovery are measured as score bands (BND-011, BND-012).

## Labels are about truth

A `must-fire` is something a careful senior reviewer would flag before this service goes to production; a
`must-not-fire` is a site a careless rule would flag and that reviewer would not. Neither was chosen to match a
scanner. Several plants carry a *posture* concept (`outbound-http-resilience`, `versioned-schema-migrations`,
`observability`, `https-enforcement`, …): readiness defects are posture failures that nonetheless have one site a
reviewer would point at, and the key names that site. A scanner that only scores the posture repository-wide cannot
hit those plants; that is a real limit of such a scanner, not a reason to drop the site.

## Plants (`must-fire`)

| Id | Concept | Where | What and why |
|---|---|---|---|
| RDY-001 | outbound-http-resilience | `Infrastructure/DependencyInjection.cs` | The merchant-webhook `HttpClient` has no resilience handler, no retry, no breaker and no explicit timeout, beside a carrier client that has the standard pipeline. |
| RDY-002 | missing-cancellation-propagation | `Infrastructure/Persistence/ParcelStore.cs` | `FindByTrackingNumberAsync` takes a `CancellationToken` and does not pass it to the EF Core query. |
| RDY-003 | missing-cancellation-propagation | `Worker/TrackingPoller.cs` | No graceful shutdown: `while (true)`, `stoppingToken` never read, `Task.Delay` without a token, and a `PollOnceAsync` helper that accepts no token. |
| RDY-004 | missing-authorization | `Api/Controllers/ReportsController.cs` | The reports controller has no `[Authorize]` and there is no fallback policy: any merchant's delivery figures are anonymous (the `reports:read` policy exists and is never applied). |
| RDY-005 | inbound-input-validation | `Api/Contracts/RedirectParcelRequest.cs` | The one request model with no validation attributes (oversized pickup point id, past hold date, unbounded note; only the implicit non-null check applies). |
| RDY-006 | observability | `Worker/NotificationDispatcher.cs` | Webhook deliveries and failures go to `Console.WriteLine` instead of the injected `ILogger`. |
| RDY-007 | empty-catch-block | `Infrastructure/Health/CarrierApiHealthCheck.cs` | The carrier health check swallows every exception and reports Healthy. |
| RDY-008 | versioned-schema-migrations | `Worker/Program.cs` | `EnsureCreated()` at worker startup against the database the API evolves with migrations. |
| RDY-009 | ci-test-gate-integrity | repository (`.github/workflows/ci.yml`) | Coverage is collected and uploaded on every CI run and never compared with a threshold. Repository-level: a CI gate is a property of the pipeline, and scanners report it without a site. |
| RDY-010 | security-response-headers | `Api/Hosting/ApiPipeline.cs` | The `/labels` branch (PDFs opened in browsers) is mapped before the security-headers middleware. |
| RDY-011 | https-enforcement | `Api/Hosting/ApiPipeline.cs` | HSTS is configured (`AddHsts`) but `UseHsts()` is never called: production sends no Strict-Transport-Security. |
| RDY-012 | non-structured-log-message | `Infrastructure/Carriers/CarrierApiClient.cs` | One warning logged with an interpolated string. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | missing-authorization | `Api/Hosting/ApiPipeline.cs` | `/health/live` and `/health/ready` are anonymous so the kubelet can probe them; they return no data. |
| TRP-002 | missing-authorization | `Api/Hosting/ApiPipeline.cs` | `/.well-known/security.txt` (RFC 9116) must be public. |
| TRP-003 | outbound-http-resilience | `Infrastructure/DependencyInjection.cs` | The carrier client uses `AddStandardResilienceHandler`. |
| TRP-004 | outbound-http-resilience | `Client/ServiceCollectionExtensions.cs` | A library returns the `IHttpClientBuilder` so the host chooses the resilience policy; hard-wiring retries in a library is the wrong design. |
| TRP-005 | missing-cancellation-propagation | `Infrastructure/Carriers/CarrierDirectory.cs` | `CancellationToken.None` on a stale-while-revalidate background refresh that must outlive the request; the comment says why. |
| TRP-006 | observability | `Cli/Program.cs` | Console output in a command-line tool is its user interface. |
| TRP-007 | versioned-schema-migrations | `.github/workflows/release.yml` | `dotnet ef migrations bundle` in the release workflow is the versioned migration path. |
| TRP-008 | versioned-schema-migrations | `tests/ParcelTracking.IntegrationTests/TrackingApiFactory.cs` | `EnsureCreated` on a throw-away SQLite database in a test fixture. |
| TRP-009 | non-structured-log-message | `Worker/TrackingPoller.cs` | A template split into two string literals joined by `+`: one compile-time constant with placeholders. |
| TRP-010..012 | nullable-analysis-disabled | `Infrastructure/Migrations/*.cs` | EF Core-generated files carry `#nullable disable`; generated code is outside nullable analysis by design. |
| TRP-013 | empty-catch-block | `Worker/NotificationDispatcher.cs` | `catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }` — the idiomatic shutdown exit. |
| TRP-014, TRP-015 | missing-image-healthcheck | `Api/Dockerfile`, `Worker/Dockerfile` | The images run only on Kubernetes, which ignores `HEALTHCHECK`; probes are in the Deployments. |
| TRP-016 | observability | `Client` project | A thin published client library has nothing of its own to log; `IHttpClientFactory` logs its requests. |
| TRP-017 | outbound-http-resilience | `Infrastructure/DependencyInjection.cs` | The readiness probe's carrier client: a 2 s timeout and deliberately no retry, so the probe reports the dependency's state now. |

## Contested truths, and how they were decided

- **RDY-001 and the 100-second default.** `HttpClient` does have a default timeout (100 s). A reviewer still flags
  the webhook client: 100 s per attempt on a worker that dispatches sequentially, no retry for a transient failure
  of an endpoint the team does not control, and no breaker for a merchant that is down. Its neighbour shows the bar
  the team itself set.
- **RDY-002 vs RDY-003.** Two different defects of one concept: a token *dropped* by a method that has one, and a
  host loop that has *no* shutdown path at all. A scanner that only checks whether an async method declares a token
  parameter can see the second (through `PollOnceAsync`) and not the first.
- **TRP-005, `CancellationToken.None`.** Passing `None` is a defect when it discards a token the caller had for the
  same unit of work. Here the work deliberately outlives the request; the request token would cancel it the moment the
  response is written. The bound on the work is the HTTP client's total timeout.
- **RDY-009 is repository-level.** CI posture is reported per pipeline, typically without a line; the rationale names
  the file and step.

## Certified clean

Every other tracked file is labelled `clean` for every concept (files that contain a plant are clean for every concept
except the planted ones). A repository-level `clean` entry certifies dependencies (current, supported, locked,
non-vulnerable), the target framework (`net10.0`), nullable analysis, CI action pinning and image pinning.

## Not applicable

Accessibility and frontend concepts (no markup, no JavaScript), BDD, event sourcing and command buses (none exist).

## Score bands

Posture concepts are banded from intent before any scan (`BND-*`): CI (high), observability (good, not top),
security tooling (good), release hygiene (thin changelog), outbound resilience and schema migrations (middling — half
done), library versioning (good), benchmark discipline (good, not in CI), disclosure policy (high), deployment and
rollback (high), disaster recovery (documented), CI gate honesty (middling), authorization, headers, HTTPS and
validation (each dented by one plant), lock files (high).

## Not covered here

Code-health metrics, architecture, domain modelling, security injection, secrets and dependency vulnerabilities are
measured by their own benchmark repositories; this one keeps them clean.
