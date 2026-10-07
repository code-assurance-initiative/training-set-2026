# 3. Credentials only from the secret store

- Status: Accepted
- Date: 2026-08-26
- Deciders: Lucia Brennan, Sami Oyelaran

## Context

On 2026-08-18 the e-mail provider's API key was committed in `appsettings.json` so that the staging environment could
send e-mail; it was found by the provider's leaked-key alert a week later and revoked (see the incident note
`docs/incidents/2026-08-25-email-provider-key.md`). Staging had no other way to get the key: the worker's manifest did
not pass it, and nobody owned adding it.

## Decision

Every credential the worker uses (database, broker, e-mail provider, SMS gateway) lives in the platform's secret store
and reaches the pod only through an ExternalSecret and a `secretKeyRef`, in every environment including staging. The
configuration files hold an empty value for each credential, and the options validation refuses to start without
one. Developers use `dotnet user-secrets` with sandbox keys. CI scans the commits of every push and pull request for
secrets.

## Consequences

- Positive: no credential in the repository or its future history; one place to rotate a key; staging is configured
  like production.
- Negative: a new environment needs its secrets provisioned before the worker starts (it refuses to start, which is
  intended); the secret scan covers new commits only — the revoked key remains readable in the repository's history,
  which is accepted because it no longer authenticates.
