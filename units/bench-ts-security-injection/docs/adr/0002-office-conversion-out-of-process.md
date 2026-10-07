# 2. Office conversion and thumbnails out of process

Date: 2026-09-23

## Status

Accepted

## Context

Exports must produce PDF and other office formats from the stored originals, and result lists show page thumbnails.
No JavaScript library converts office documents faithfully.

## Decision

Convert with LibreOffice (`soffice --headless --convert-to`) and render thumbnails with ImageMagick, both as child
processes with a configured binary path and a timeout (`CONVERSION_TIMEOUT_SECONDS`). Each export converts into its
own working directory under `STORAGE_ROOT/exports`, named by a generated id.

## Consequences

- The host (or container image) must provide LibreOffice and ImageMagick.
- A hung conversion is killed at the timeout and reported as a failed export.
- Tests run the converters against stand-in scripts.
