import type { Knex } from 'knex';

export async function up(db: Knex): Promise<void> {
  await db.schema.createTable('members', (table) => {
    table.uuid('id').primary();
    // Personal data: null once the member is erased. Phone and date of birth are stored only
    // encrypted (ADR 0004); the e-mail address is the log-in and lookup key (data inventory).
    table.text('first_name');
    table.text('last_name');
    table.text('email').unique();
    table.text('phone_encrypted');
    table.text('date_of_birth_encrypted');
    table.text('status').notNullable();
    table.date('membership_ends_on').notNullable();
    table.timestamp('registered_at', { useTz: true }).notNullable();
    table.timestamp('cancelled_at', { useTz: true });
    table.timestamp('erased_at', { useTz: true });
  });

  await db.schema.createTable('member_consents', (table) => {
    table.uuid('member_id').notNullable().references('members.id');
    table.text('purpose').notNullable();
    table.boolean('granted').notNullable();
    table.timestamp('recorded_at', { useTz: true }).notNullable();
    table.primary(['member_id', 'purpose']);
  });

  await db.schema.createTable('class_sessions', (table) => {
    table.uuid('id').primary();
    table.text('title').notNullable();
    table.timestamp('starts_at', { useTz: true }).notNullable();
    table.integer('duration_minutes').notNullable();
    table.integer('capacity').notNullable();
  });

  await db.schema.createTable('class_bookings', (table) => {
    table.uuid('id').primary();
    table.uuid('session_id').notNullable().references('class_sessions.id');
    table.uuid('member_id').notNullable().references('members.id');
    table.text('status').notNullable();
    table.timestamp('booked_at', { useTz: true }).notNullable();
    table.timestamp('cancelled_at', { useTz: true });
    table.timestamp('reminded_at', { useTz: true });
  });

  await db.schema.createTable('reminder_deliveries', (table) => {
    table.uuid('id').primary();
    table.uuid('booking_id').notNullable().references('class_bookings.id');
    table.text('channel').notNullable();
    table.text('recipient').notNullable();
    table.timestamp('sent_at', { useTz: true }).notNullable();
  });
}

export async function down(db: Knex): Promise<void> {
  await db.schema.dropTable('reminder_deliveries');
  await db.schema.dropTable('class_bookings');
  await db.schema.dropTable('class_sessions');
  await db.schema.dropTable('member_consents');
  await db.schema.dropTable('members');
}
