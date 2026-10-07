# 2. Convert documents with LibreOffice, out of process

Date: 2026-09-21

## Status

Accepted

## Context

Reports are exported as PDF, DOCX or ODT, and uploaded attachments need PDF previews and thumbnails. No managed
library converts every office format we receive with acceptable fidelity.

## Decision

Run LibreOffice (`soffice --headless --convert-to`) and ImageMagick as child processes on the API host, one process
per conversion, with a per-conversion working directory under the configured scratch root and a timeout.

## Consequences

- Any value that reaches a command line is part of the process boundary: the executable must be fixed and arguments
  must be passed as an argument list, never through a shell.
- The container image carries LibreOffice and ImageMagick; conversions are CPU-bound and are limited to two in flight.
