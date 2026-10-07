import type { Knex } from 'knex';

export async function up(db: Knex): Promise<void> {
  await db.schema.createTable('audit_log', (table) => {
    table.uuid('id').primary();
    table.timestamp('occurred_at', { useTz: true }).notNullable();
    table.text('actor').notNullable();
    table.text('action').notNullable();
    table.text('subject_type').notNullable();
    table.uuid('subject_id').notNullable();
    table.jsonb('changed_fields').notNullable();
    table.index(['subject_type', 'subject_id']);
  });

  await db.schema.createTable('outbox_messages', (table) => {
    table.uuid('id').primary();
    table.text('type').notNullable();
    table.jsonb('payload').notNullable();
    table.timestamp('occurred_at', { useTz: true }).notNullable();
  });
}

export async function down(db: Knex): Promise<void> {
  await db.schema.dropTable('outbox_messages');
  await db.schema.dropTable('audit_log');
}
