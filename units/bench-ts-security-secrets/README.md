# media-intake

A small Node.js service that accepts media uploads from a browser page, stores them in S3-compatible object storage,
records them in Postgres and tells interested parties about them: a signed webhook to a subscriber, a push
notification through Firebase Cloud Messaging, an optional e-mail with a short-lived download link, and Slack for
operations. A nightly job reports stored gigabyte-hours to Stripe metered billing and posts an upload digest to Slack.
A transcoding vendor renders thumbnails for images and video.

> This repository is a unit of a scanner benchmark: it deliberately contains committed secrets (generated fakes that
> authenticate nothing). See [`benchmark/README.md`](benchmark/README.md) before reusing anything from it.

## Build and run

Requirements: Node.js 22 (see `.nvmrc`) and npm; Docker for the local database.

```sh
npm ci
npm run build          # TypeScript → dist/
npm run build:web      # the upload page → web/dist/
docker compose up -d db
npm start
```

Configuration comes from environment variables (see `.env.example` and `src/config.ts`); `dotenv` fills in
anything not set from `.env`. `UPLOAD_SIGNING_SECRET` and `STRIPE_RESTRICTED_KEY` are required.
The nightly job runs as `node dist/nightly.js` from the platform's scheduler.

## Testing

```sh
npm test               # vitest: unit tests and in-process HTTP tests (Fastify inject)
npm run typecheck      # strict TypeScript over src/, tests/ and web/
```

The tests stub `fetch` and replace object storage and Postgres with in-memory doubles, so they need no network.

## Architecture

`src/app.ts` builds the Fastify application; `src/server.ts` wires the real adapters (S3 client, Postgres pool,
MongoDB audit log, transcoder, notifiers) and listens. Uploads are authenticated by an HMAC over owner, timestamp
and body digest computed in the browser; downloads by a five-minute HS256 token scoped to one object. See
[`docs/architecture.md`](docs/architecture.md) and the decision records in [`docs/adr/`](docs/adr/).

## API

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/v1/media` | multipart upload; headers `x-owner-id`, `x-upload-timestamp`, `x-upload-signature`, optional `x-notify-email` |
| `POST` | `/v1/media/:id/download-token` | issue a download token for the owner (`x-owner-id`) |
| `GET` | `/v1/media/:id?token=…` | download the object |
| `POST` | `/v1/plans/upgrade-intent` | create the payment intent for a storage plan upgrade |
| `GET` | `/health` | liveness |

A download-token response looks like this (the token below expired in November 2025):

```json
{ "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvd25lciI6InN0dWRpby1ub3J0aCIsInN1YiI6IjBiNmY1YzFlLTJmNDMtNGE3Ny05YjhlLTVkMGM3YTFmMmUzOSIsImlzcyI6Im1lZGlhLWludGFrZSIsImF1ZCI6Im1lZGlhLWRvd25sb2FkIiwiaWF0IjoxNzYyMTYxMjQ3LCJleHAiOjE3NjIxNjE1NDd9.BpcFmdJ0MQH5Hk6BibBwFBlP0d9sxVLa7TEh-zTQ4MY", "expiresInSeconds": 300 }
```

Calling the transcoder by hand uses your own token: `Authorization: Bearer <token>`. Installing the organisation's
private packages needs `//npm.pkg.github.com/:_authToken=${NPM_TOKEN}` in your user-level `~/.npmrc`.

## Releasing

Update `CHANGELOG.md` and the version in `package.json`, then `npm run release`.

## Licence

MIT — see [`LICENSE`](LICENSE).
