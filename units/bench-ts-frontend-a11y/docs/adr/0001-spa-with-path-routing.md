# ADR 0001: A Vite single-page app with path-based routing and no router library

- Status: accepted
- Date: 2026-09-14

## Context

The catalogue has six pages and one nested state (an item opened over the search results). The library's web server
already serves `index.html` for unknown paths. Bookmarkable item links matter: staff paste them into newsletters.

## Decision

Build a single-page application with Vite, React and TypeScript. Route on `window.location.pathname` with a small
`parseRoute` function and a `useRoute` hook; in-app links push history entries. Do not add a router library.

## Consequences

- Routes are plain data (`Route` union) and are unit-tested without rendering.
- No nested layouts or loaders; if the app grows past a handful of pages we revisit this decision.
- Fragment links (`#main-content`) keep their normal browser meaning because routing does not use the hash.
