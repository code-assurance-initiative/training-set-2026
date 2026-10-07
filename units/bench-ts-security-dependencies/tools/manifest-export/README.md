# manifest-export

Turns a dispatch run exported from Depot Dispatch (`GET /api/runs/{id}/export`) into the CSV manifest the depot label
printers read: one line per stop, vehicle by vehicle in loading order, `;`-separated, CRLF line ends.

```bash
npm ci --omit=dev
./bin/manifest-export.js --input run-2026-11-02-AAR.json            # writes manifest-2026-11-02-AAR.csv beside it
./bin/manifest-export.js -i run.json -o /mnt/printer/manifest.csv -d ,
```

It runs on the depot PCs, which run Node.js 16 (`engines`). Tests: `npm test` (the built-in `node:test` runner).
