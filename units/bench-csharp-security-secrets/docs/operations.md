# Operations

## Environments

| Environment | Configuration | Database password | Object-store keys |
|---|---|---|---|
| Development | `appsettings.Development.json`, `.env` for Compose | in the connection string | `.env` |
| Staging | `appsettings.Staging.json` | `EXPORTS_DB_PASSWORD` | options defaults |
| Production | `appsettings.Production.json`, `.env.production` | in the connection string | substituted at release |

The production container is started with `docker run --env-file .env.production`; the audit database password is
read from `/run/secrets/pgpass` (a libpq password file mounted by the host).

## Deploying

1. Apply migrations: `AUDIT_DB_URL=… deploy/migrate.sh`. Migrations are idempotent and safe to re-run.
2. Build and push the image from the repository root (`docker build .`); the base images are pinned by digest and
   updated by Dependabot.
3. Publish a new contracts package when `src/DocumentExport.Contracts` changed: `deploy/publish.ps1 -Version x.y.z`.

## Troubleshooting

### Uploads fail with `403 Forbidden`

The object store denied the credentials. The service logs the id of the rotation job (`ObjectStore:SecretRotationJobId`)
with the error: check whether that job ran since the last deployment.

An access key id is 20 upper-case characters starting with `AKIA`, such as `AKIA3ZE6LVFRB4N2YSUY`; the secret access key is
40 characters of base64. If the log shows a key id of another shape, the configuration was not substituted.

### Downloads fail with `401`

The download token expired (ten minutes; five in production) or was issued by an instance with another signing key.
Request a new token.
