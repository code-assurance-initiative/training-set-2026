# Benchmark: infrastructure-as-code, container and CI-workflow security

This repository is one unit of the scanner benchmark in
[`code-assurance-initiative/scanner-benchmark`](https://github.com/code-assurance-initiative/scanner-benchmark).
Its labels are in [`answer-key.json`](answer-key.json) (format: `scanner-benchmark/docs/CONTRACT.md`); its
authoring log is [`journal.md`](journal.md).

## Safety note

**Every credential in this repository is a generated fake.** The two chat bot tokens (`xoxb-` format) were produced
with Python `secrets` in the real format of their kind and authenticate nothing; the sealed-secret ciphertext and the
image digests are random bytes. There is no workspace, registry, cluster or database behind any of them, and nothing
here is ever deployed. Do not report them; do not reuse them.

## Theme

A small ASP.NET Core (`net10.0`) **dock-booking service** for a distribution depot: an HTTP API that books loading
docks for carriers (PostgreSQL, JWT bearer authentication), and a reminder worker that posts upcoming arrivals to the
yard team's chat channel. Around it sits what a real team ships: two Dockerfiles, a Compose stack for local
development, Kubernetes manifests (Namespace, Deployments, Service, Ingress, ServiceAccounts, NetworkPolicies, an
ExternalSecret, a SealedSecret, a committed `kind: Secret`, a log-forwarder DaemonSet with its RBAC) and six GitHub
Actions workflows. The defects sit where teams really put them - the hurried second image, the third-party log agent,
the convenience workflows - while the API, its image and the build workflow are done properly.

The C# code is shared scaffolding (README, ADRs, architecture doc, CHANGELOG, SECURITY.md, central package management
with lock files, nullable, `src/` + `tests/`, 33 passing tests). It is certified clean for this repository's concepts;
it is not where the theme lives. `ci.yml` and `codeql.yml`, the workflows that build and analyse the app, are correct:
the planted workflow defects live in separate release/deploy/preview/triage workflows.

Terraform, Helm and CloudFormation are not covered (the matrix assigns none to this repository).

## Labels are about truth

A `must-fire` is something a careful reviewer of deployment configuration would flag. A `must-not-fire` is a site a
careless rule would flag and that reviewer would not. Neither label was chosen to match any scanner.

Concepts (from `scanner-benchmark/taxonomy.json`): `container-excessive-privilege` (CWE-250),
`container-missing-resource-limits` (CWE-770), `mutable-image-reference`, `iac-misconfiguration`,
`hardcoded-credential` (CWE-798), `cleartext-transmission` (CWE-319), `ci-workflow-injection` (CWE-78),
`ci-secret-exposure`, `secret-in-process-arguments` (CWE-214), `ci-token-excessive-permissions` (CWE-250),
`unpinned-ci-action` (CWE-829). Defects that are absences (no USER, no probes, no limits, no securityContext) are
located on the block that lacks them: the container entry, or the whole Dockerfile when the property is absent from the entire file.

## Plants (`must-fire`)

`RD` = `src/Depot.Slots.Reminders/Dockerfile`, `DS` = `deploy/k8s/node-agent/daemonset.yaml`,
`RM` = `deploy/k8s/reminders/deployment.yaml`.

| Id | Concept | Where | What and why it is a defect |
|---|---|---|---|
| IAC-001 | excessive privilege | `RD` (whole file) | No `USER` anywhere: the worker runs as root (the base image default). |
| IAC-002 | mutable image | `RD` | `FROM mcr.microsoft.com/dotnet/aspnet:latest`. |
| IAC-003 | credential | `RD` | Test-workspace chat bot token baked in with `ENV Chat__BotToken=xoxb-…` - readable from the image, shipped to production. |
| IAC-004 | excessive privilege | `DS` | `hostNetwork: true` on a log forwarder that only tails files; also escapes NetworkPolicy. |
| IAC-005 | excessive privilege | `DS` | `privileged: true` on the forwarder container. |
| IAC-006 | excessive privilege | `DS` | The node's containerd socket mounted read-write via `hostPath` (runtime control = node root). |
| IAC-007 | mutable image | `DS` | `cr.fluentbit.io/fluent/fluent-bit:latest`. |
| IAC-008 | excessive privilege | `RM` container | No `securityContext` at all: escalation allowed, capabilities kept, writable root FS. |
| IAC-009 | resource limits | `RM` container | No requests or limits. |
| IAC-010 | misconfiguration | `RM` container | No liveness/readiness probes although the worker serves `/health/*` on its declared port. |
| IAC-011 | misconfiguration | `RM` pod spec | Default service account with its token automounted into a pod that never calls the API server. |
| IAC-012 | credential | `deploy/k8s/reminders/secret.yaml` | Committed `kind: Secret` with the production bot token base64-encoded under `data:`. |
| IAC-013 | cleartext | `deploy/k8s/api/ingress.yaml` | Ingress without `tls:` - the API and its bearer tokens over plain HTTP. |
| IAC-014 | misconfiguration | `deploy/k8s/node-agent/rbac.yaml` | ClusterRole `apiGroups/resources/verbs: ["*"]` for a forwarder that needs get/list/watch on pods. |
| IAC-015 | token permissions | `.github/workflows/release.yml` | `permissions: write-all`. |
| IAC-016 | unpinned action | `.github/workflows/release.yml` | `docker/setup-buildx-action@v3` (third-party, mutable tag) in the job holding the push token. |
| IAC-017 | secret exposure | `.github/workflows/pr-preview.yml` | `pull_request_target` checks out the PR head and `docker build`s it in a step that holds the registry token. |
| IAC-018 | secret in argv | `.github/workflows/deploy.yml` | `kubectl --token "${{ secrets.KUBE_DEPLOY_TOKEN }}"` - interpolated into the script and passed on the command line. |
| IAC-019 | workflow injection | `.github/workflows/issue-triage.yml` | `title="${{ github.event.issue.title }}"` in a `run:` script. |

## Traps (`must-not-fire`)

| Id | Concept | Where | Why it is not a defect |
|---|---|---|---|
| TRP-001 | mutable image | `src/Depot.Slots.Api/Dockerfile` | Digest-pinned `FROM` under a comment that mentions the *latest* 10.0 tag. |
| TRP-002 | mutable image | `src/Depot.Slots.Api/Dockerfile` | `aspnet:10.0@sha256:…` - the digest wins. |
| TRP-003 | excessive privilege | `src/Depot.Slots.Api/Dockerfile` | `USER 1654:1654` - a numeric non-root user. |
| TRP-004 | misconfiguration | `src/Depot.Slots.Api/Dockerfile` (whole file) | No `HEALTHCHECK` on an image that only runs on Kubernetes (which ignores it and uses probes). |
| TRP-005 | misconfiguration | `RD` (whole file) | Same, for the worker image (its missing probes are a defect - in the manifest, IAC-010). |
| TRP-006 | mutable image | `deploy/k8s/api/deployment.yaml` | Image by digest. |
| TRP-007 | excessive privilege | `deploy/k8s/api/deployment.yaml` | `runAsUser: 1654` - non-root, below 10000; not a privilege. |
| TRP-008 | misconfiguration | `deploy/k8s/api/deployment.yaml` | Probes defined through a YAML anchor and a `<<:` merge key. |
| TRP-009 | excessive privilege | `deploy/k8s/api/deployment.yaml` | `emptyDir` scratch volume, not a host path. |
| TRP-010 | credential | `deploy/k8s/api/deployment.yaml` | Connection string from `secretKeyRef`. |
| TRP-011 | credential | `deploy/k8s/api/external-secret.yaml` | ExternalSecret: a reference into the secret store, no value. |
| TRP-012 | credential | `deploy/k8s/api/ghcr-pull.sealedsecret.yaml` | SealedSecret ciphertext - high entropy, safe by design. |
| TRP-013 | secret exposure | `.github/workflows/pr-labeler.yml` | `pull_request_target` that never checks out or runs PR code. |
| TRP-014 | token permissions | `.github/workflows/ci.yml` | `permissions: contents: read`. |
| TRP-015 | unpinned action | `.github/workflows/ci.yml` | First-party `actions/*` pinned by 40-hex SHA. |
| TRP-016 | secret in argv | `.github/workflows/release.yml` | Secret via `env:` and `--password-stdin`; never on a command line. |
| TRP-017 | credential | `RM` | `Chat__BotToken` from `secretKeyRef`. |
| TRP-018 | workflow injection | `.github/workflows/pr-preview.yml` | `github.event.pull_request.number` (an integer) via `env:`. |
| TRP-019 | workflow injection | `.github/workflows/pr-preview.yml` | Same, in the comment step. |
| TRP-020 | workflow injection | `.github/workflows/deploy.yml` | `inputs.version` used as checkout `ref:`, not in a shell. |

## Contested truths, and how they were decided

- **IAC-018 is `secret-in-process-arguments`, not `ci-secret-exposure`.** The token is interpolated into the run
  script *and* lands in kubectl's argv. The taxonomy's `ci-secret-exposure` is about secrets reaching untrusted code
  (fork builds, third-party steps); nothing untrusted runs here. The harm is the token in the generated script and the
  process list, which is CWE-214.
- **IAC-013 is a defect although the host is internal.** The API carries bearer tokens; the architecture document
  promises HTTPS; an internal network is not an encrypted one.
- **The read-only `/var/log` hostPath of the log forwarder is deliberately unlabelled.** Reading node log files is
  what a log forwarder is for and is the conventional exemption; Pod Security "baseline" nevertheless forbids every
  hostPath. Reasonable reviewers differ, so the site is neither a plant nor a trap, and that file carries no clean
  certificate. The containerd socket mount beside it (IAC-006) is not contested.
- **TRP-007 (`runAsUser: 1654`).** Some rule sets ask for UIDs above 10000 to avoid colliding with host accounts.
  With runAsNonRoot, no privilege escalation, all capabilities dropped and a read-only root filesystem, the UID's
  numeric range grants nothing; it is a preference, not a defect.
- **TRP-004/TRP-005 (no HEALTHCHECK).** Kubernetes does not run Dockerfile health checks. Health is the
  Deployment's job, and where the Deployment omits it, that is the defect (IAC-010).

## What is clean

Every file without a plant is certified free of the concepts above (`CLN-*`, whole file), including the trap files.
Files that carry a plant have no clean certificate.

## Posture and judged properties (`score-band`, 0-100)

| Id | Concept | Band | Why |
|---|---|---|---|
| BND-001 | network-egress-policy | 60-100 | Default-deny ingress + egress with per-flow allowances; the hostNetwork forwarder escapes it. |
| BND-002 | workload-syscall-confinement | 10-60 | seccomp RuntimeDefault on one of three workloads; no AppArmor/SELinux. |
| BND-003 | runtime-threat-detection-and-admission | 0-30 | No Falco/Tetragon, no admission policy, no Pod Security labels. |
| BND-004 | deployment-rollback-safety | 50-100 | Automated deploy, RollingUpdate with maxUnavailable 0, probes on the API, a reviewer-gated environment; the worker lacks probes. |
| BND-005 | disaster-recovery-evidence | 0-40 | State is in a managed database outside the repository; no backup/restore/RTO evidence here. |
| BND-006 | environment-separation | 30-90 | Per-environment settings, but the worker image bakes a test-workspace token into production. LLM-judged. |
| BND-007 | build-provenance-and-signing | 0-40 | Images pushed with provenance disabled, no SBOM, no signing; one unpinned action. |
