# Security policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately through GitHub's
[private vulnerability reporting](https://github.com/code-assurance-initiative/bench-csharp-readiness/security/advisories/new)
for this repository. Do not open a public issue. The same contact is published by the API at
`/.well-known/security.txt` (RFC 9116).

Include what you found, how to reproduce it, and the impact you expect. We acknowledge reports within three working
days and aim to publish a fix or a mitigation within 30 days of confirming the issue. We will credit you in the advisory
unless you ask us not to.

## Supported versions

Only the latest release is supported.

## Scope

In scope: the API, the worker, the client library, the command-line tool, and the build, release and deployment
configuration in this repository. Out of scope: the identity provider that issues access tokens, the carrier API and
the merchant-hooks relay (report those to their operators), and denial of service through request volume (the API runs
behind a gateway that applies rate limits).
