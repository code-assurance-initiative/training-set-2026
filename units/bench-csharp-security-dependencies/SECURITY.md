# Security policy

## Reporting a vulnerability

Please report suspected vulnerabilities privately, through GitHub's
[private vulnerability reporting](https://github.com/code-assurance-initiative/bench-csharp-security-dependencies/security/advisories/new)
for this repository. Do not open a public issue.

We acknowledge reports within three working days and aim to publish a fix or mitigation within 30 days.

## Supported versions

Only the latest release of the invoicing bundle receives security fixes.

## Known dependency advisories

This repository is a scanner benchmark: several dependencies are pinned, on purpose, to versions with published
advisories, and the archive exporter targets an end-of-life runtime. They are listed in `benchmark/README.md`. Please
do not report them; reports about anything else are welcome.
