import { randomUUID } from 'node:crypto';
import type { Knex } from 'knex';
import type { Logger } from 'pino';
import type { IntegrationBus, IntegrationMessage } from './integration-bus.js';

/** Writes integration messages in the caller's transaction, so they exist exactly when the change does. */
export class Outbox {
  constructor(private readonly db: Knex) {}

  async add(type: string, payload: object, occurredAt: Date): Promise<void> {
    await this.db('outbox_messages').insert({
      id: randomUUID(),
      type,
      payload: JSON.stringify(payload),
      occurred_at: occurredAt,
    });
  }
}

interface OutboxRow {
  id: string;
  type: string;
  payload: string | Record<string, unknown>;
  occurred_at: Date | string;
}

/**
 * Publishes committed outbox messages in order and deletes each one after its handlers ran, so the
 * personal data some messages carry does not outlive their delivery. A crash in between publishes the
 * message again: subscribers are idempotent.
 */
export class OutboxDispatcher {
  constructor(
    private readonly db: Knex,
    private readonly bus: IntegrationBus,
    private readonly logger: Logger,
  ) {}

  async dispatchPending(batchSize = 50): Promise<number> {
    const rows = await this.db<OutboxRow>('outbox_messages')
      .orderBy('occurred_at')
      .limit(batchSize);
    for (const row of rows) {
      await this.bus.publish(toMessage(row));
      await this.db('outbox_messages').where({ id: row.id }).delete();
    }
    if (rows.length > 0) {
      this.logger.debug({ count: rows.length }, 'Outbox messages dispatched');
    }
    return rows.length;
  }
}

function toMessage(row: OutboxRow): IntegrationMessage {
  const payload: Record<string, unknown> =
    typeof row.payload === 'string'
      ? (JSON.parse(row.payload) as Record<string, unknown>)
      : row.payload;
  return {
    id: row.id,
    type: row.type,
    occurredAt: new Date(row.occurred_at).toISOString(),
    payload,
  };
}
