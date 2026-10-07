# Security policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately through GitHub's
[private vulnerability reporting](https://github.com/code-assurance-initiative/bench-ts-security-dependencies/security/advisories/new)
for this repository. Do not open a public issue.

Include what you found, how to reproduce it, and the impact you expect. We acknowledge reports within three working
days and aim to publish a fix or a mitigation within 30 days of confirming the issue. We will credit you in the advisory
unless you ask us not to.

## Supported versions

Only the latest commit on `main` is supported.

## Scope

In scope: the service and `tools/manifest-export`, their dependencies as locked in the two `package-lock.json` files,
and the build and CI configuration. Out of scope: the depot identity service that issues terminal tokens, the reverse
proxy that terminates TLS on the depot server, the carrier's and the geocoding provider's own services, and denial of
service through request volume from inside the depot network.
