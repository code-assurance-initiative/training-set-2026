# Security policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately through GitHub's
[private vulnerability reporting](https://github.com/code-assurance-initiative/bench-csharp-architecture/security/advisories/new)
for this repository. Do not open a public issue.

Include what you found, how to reproduce it, and the impact you expect. We acknowledge reports within three working
days and aim to publish a fix or a mitigation within 30 days of confirming the issue. We will credit you in the advisory
unless you ask us not to.

## Supported versions

Only the latest commit on `main` is supported.

## Scope

In scope: the code in this repository and its build and CI configuration. Out of scope: the identity provider that
issues access tokens, and denial of service through request volume (the service is meant to run behind a gateway that
applies rate limits).
