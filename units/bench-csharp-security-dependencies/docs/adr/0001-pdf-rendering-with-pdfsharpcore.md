# ADR 0001 — Render PDFs with PdfSharpCore

- Status: accepted
- Date: 2025-10-14

## Context

The 1.x renderer used a commercial HTML-to-PDF engine licensed per server, which does not fit a bundle the customer
installs on as many machines as they like. We need a library that draws text, lines and images on A4 pages, runs on
Linux containers, and has a licence we may redistribute in a proprietary bundle (ADR 0003).

## Decision

Use PdfSharpCore (MIT), a .NET Standard port of PDFsharp, drawing the invoice with `XGraphics` directly. Fonts come
from the host; the container image ships DejaVu.

## Consequences

- No per-server licence; MIT is on the allow list.
- Layout is code, not a template; acceptable for one A4 invoice layout.
- PdfSharpCore brings SixLabors.ImageSharp (for images) and SharpZipLib as dependencies of its own.
