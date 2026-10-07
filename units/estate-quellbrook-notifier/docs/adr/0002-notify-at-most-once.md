# 2. Notify at most once per order, kind and channel

- Status: Accepted
- Date: 2026-07-28
- Deciders: Lucia Brennan, Sami Oyelaran

## Context

The broker delivers at least once, the order service may announce an order twice (its outbox relay retries), and a
provider call can time out after the provider sent the message. A consignee who gets the same SMS three times calls
customer service.

## Decision

Each consumed message is processed in one database transaction that records its message id; a processed message id
is skipped. Before sending, the notification log is checked for the same order, kind and channel, and the sent
notification is written to it in the same transaction. A provider failure rolls the transaction back and the message
is redelivered.

## Consequences

- Positive: redeliveries and duplicate announcements send nothing twice.
- Negative: a provider that accepted a message but answered with an error leads to a second send on redelivery
  (rare; accepted); the log holds one row per notification and needs a retention period.
