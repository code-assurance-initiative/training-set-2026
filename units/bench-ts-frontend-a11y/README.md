# Community Library Catalogue

The public web catalogue of a community library with three branches. Visitors search the catalogue by title, author or
subject, narrow results by format and availability, open an item to read its record, and reserve it for pickup at a
branch. Signed-in members see their loans in a sortable table, renew them one by one or all at once, export them as
CSV and check their fines. The site also shows upcoming events, the recording of the latest storytime session, a photo
gallery, opening hours and the current staff notice, and it remembers a few preferences (compact results, loan
reminders, home branch) in the browser.

It is a single-page application written in TypeScript with React 19 and built with Vite. It talks to the library's
catalogue API over JSON under `/api`; photos, covers and videos are served by the library's media server under
`/media`.

## Build and run

Requires Node.js 22 (see `.nvmrc`).

```bash
npm ci
npm run dev        # development server with an in-memory demo catalogue
npm run build      # type-check and build the production bundle into dist/
npm run preview    # serve the production bundle locally
```

In development (`npm run dev`) the app uses an in-memory catalogue with a handful of items and two loans, so it runs
without the API. The production build talks to `/api` on the same origin, where the session cookie authenticates the
loan and reservation calls.

## Testing

```bash
npm test                # vitest + Testing Library in jsdom
npm run test:coverage   # the same, with coverage thresholds
npm run lint            # ESLint (typescript-eslint strict + React hooks rules)
npm run typecheck       # tsc --noEmit
npm run format:check    # Prettier
```

Tests live in `tests/` and drive components the way a visitor does — by role and visible text — against the in-memory
catalogue.

## Architecture

```
src/
  api/          CatalogueClient interface, the HTTP client and the in-memory demo client
  catalogue/    formatting helpers and the subject-heading vocabulary
  components/   site header, links, icon buttons, toast region
  context/      React contexts: catalogue client, preferences, toasts
  features/     one folder per page: home, search, item, borrow, events, gallery, loans, settings, news
  styles/       plain CSS, one file per area
```

Pages are chosen from the URL path (`routing.ts`, `useRoute.ts`) without a router library; see
[docs/architecture.md](docs/architecture.md) and the decision records in [docs/adr](docs/adr).

## Contributing

Open a pull request against `main`. CI runs formatting, lint, type-check, build and the tests with coverage
thresholds; all must pass. Security issues: see [SECURITY.md](SECURITY.md).

## Licence

MIT — see [LICENSE](LICENSE).
