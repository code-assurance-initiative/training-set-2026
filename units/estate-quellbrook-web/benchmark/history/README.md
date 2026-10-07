# Scripted history

18 commits by fictional authors, rebuilt and verified by `build-history.sh` (see its header). Commits after them are real benchmark maintenance.

## Sprint 1 (2026-07-27 – 2026-08-07, release `v0.1.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `e09dc64` | 2026-07-27 | Teodor Vasko | chore: Vite + React + TypeScript skeleton with lint, format and test tooling |  |
| `3c398af` | 2026-07-28 | Teodor Vasko | docs: ADR 0001 and ADR 0002 (a static SPA behind the gateway and the sign-in proxy) |  |
| `dade662` | 2026-07-29 | Ines Halvard | feat: layout with skip link and main navigation, path routing |  |
| `afbbbc9` | 2026-07-31 | Teodor Vasko | feat(orders): orders list and order page against the gateway | WEB-001: the orders table opens an order from a click on the row only |
| `2a84f14` | 2026-08-04 | Ines Halvard | build: nginx image with security headers and Kubernetes manifests |  |
| `eb05235` | 2026-08-05 | Mara Ellingsworth | ci: format, lint, type-check, build, test with coverage, audit; CodeQL, release and deploy workflows |  |
| `2aa242e` | 2026-08-06 | Teodor Vasko | docs: README, architecture and catalog entry |  |
| `ba63d43` | 2026-08-07 | Teodor Vasko | release 0.1.0 | tag `v0.1.0` |

## Sprint 2 (2026-08-10 – 2026-08-21, release `v0.2.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `0bf52ad` | 2026-08-11 | Ines Halvard | feat(dispatch): dispatch board with the day's routes, their vehicles and stops; starting a route | WEB-003: the vehicle icon has no text alternative |
| `fbec35a` | 2026-08-14 | Teodor Vasko | feat(orders): delivery status on the order page; cancelling an order with a reason |  |
| `767e332` | 2026-08-20 | Teodor Vasko | docs: dispatch board and order page in the README |  |
| `321dc76` | 2026-08-21 | Teodor Vasko | release 0.2.0 | tag `v0.2.0` |

## Sprint 3 (2026-08-24 – 2026-09-04, release `v0.3.0`)

| Commit | Date | Author | Subject | Note |
|---|---|---|---|---|
| `2ae20c4` | 2026-08-25 | Teodor Vasko | feat(orders): search the page by consignee or city; filter by status | WEB-002: the search box has a placeholder and no label |
| `42b5008` | 2026-08-27 | Ines Halvard | feat(dispatch): compact or comfortable cards, remembered in the browser; filter stops by postal code |  |
| `d5df1eb` | 2026-08-31 | Mara Ellingsworth | ci: sign the image's build provenance and verify it before deploying |  |
| `d8aa8f0` | 2026-09-02 | Teodor Vasko | docs: search, filters and board preferences in the README |  |
| `68c359b` | 2026-09-03 | Ines Halvard | fix(nginx): set the security headers once; locations no longer drop them |  |
| `0006829` | 2026-09-04 | Teodor Vasko | release 0.3.0 | tag `v0.3.0` |
