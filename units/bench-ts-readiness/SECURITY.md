# Security policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately through GitHub's
[private vulnerability reporting](https://github.com/code-assurance-initiative/bench-ts-readiness/security/advisories/new)
for this repository. Do not open a public issue. The service also publishes `/.well-known/security.txt`.

Include what you found, how to reproduce it, and the impact you expect. We acknowledge reports within three working
days and aim to publish a fix or a mitigation within 30 days of confirming the issue. We will credit you in the advisory
unless you ask us not to.

## Supported versions

The service: the latest release. `@parcel-tracking/client`: the latest minor of the current major.
`@parcel-tracking/webhooks`: the latest release.

## Scope

In scope: the code in this repository, its dependencies as locked in `package-lock.json`, its container image, its
Kubernetes manifests and its build and CI configuration. Out of scope: the identity provider that issues merchant
tokens, the carriers' APIs, the cluster itself, and denial of service through request volume (the API runs behind an
ingress that applies rate limits).
