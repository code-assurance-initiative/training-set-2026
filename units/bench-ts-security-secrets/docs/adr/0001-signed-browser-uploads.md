# 1. Browser uploads go straight to the API, authenticated by a request signature

Date: 2025-09-18 · Status: accepted

## Context

The upload page is used by partner studios from their own networks. We considered presigned object-store URLs
(the browser uploads to the bucket directly) and uploads through the API.

## Decision

Uploads go through the API so that content-type checks, the database row and the audit event happen before an
object becomes visible. Each request carries an HMAC-SHA256 over owner, timestamp and body digest; the API rejects
signatures older than two minutes.

## Consequences

The API carries the upload bandwidth (capped at 200 MB per file in production). Object keys are never chosen by
the client.
