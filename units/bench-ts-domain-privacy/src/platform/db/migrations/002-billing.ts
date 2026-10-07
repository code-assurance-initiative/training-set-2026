import type { Knex } from 'knex';

export async function up(db: Knex): Promise<void> {
  await db.schema.createTable('billing_accounts', (table) => {
    table.uuid('id').primary();
    table.uuid('member_id').notNullable().unique();
    table.text('holder_name');
    table.text('holder_email');
    table.date('date_of_birth');
    table.timestamp('opened_at', { useTz: true }).notNullable();
    table.timestamp('anonymised_at', { useTz: true });
  });

  await db.schema.createTable('invoices', (table) => {
    table.uuid('id').primary();
    table.uuid('account_id').notNullable().references('billing_accounts.id');
    table.text('period').notNullable();
    table.text('currency').notNullable();
    table.text('status').notNullable();
    table.date('due_on').notNullable();
    table.timestamp('issued_at', { useTz: true });
    table.timestamp('paid_at', { useTz: true });
    table.unique(['account_id', 'period']);
  });

  await db.schema.createTable('invoice_lines', (table) => {
    table.uuid('id').primary();
    table.uuid('invoice_id').notNullable().references('invoices.id');
    table.text('kind').notNullable();
    table.text('description').notNullable();
    table.integer('amount_minor').notNullable();
  });
}

export async function down(db: Knex): Promise<void> {
  await db.schema.dropTable('invoice_lines');
  await db.schema.dropTable('invoices');
  await db.schema.dropTable('billing_accounts');
}
