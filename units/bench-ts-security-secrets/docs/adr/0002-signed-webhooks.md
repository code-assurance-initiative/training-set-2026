# 2. Webhook deliveries are signed with Ed25519

Date: 2025-10-02 · Status: accepted

## Context

Subscribers must be able to tell our deliveries from forged ones without sharing a secret with each of them.

## Decision

Every delivery carries `Webhook-Timestamp` and `Webhook-Signature: ed25519=<base64>` over `<timestamp>.<body>`.
Subscribers verify with our published public key; the private key never leaves the service.

## Consequences

Rotating the key means publishing the new public key before switching; subscribers may hold both during the
overlap.
