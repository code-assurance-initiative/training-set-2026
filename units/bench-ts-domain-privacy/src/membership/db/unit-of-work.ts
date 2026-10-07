import type { Knex } from 'knex';
import { AuditLog } from '../../platform/audit-log.js';
import type { FieldCipher } from '../../platform/field-cipher.js';
import { Outbox } from '../../platform/outbox.js';
import type {
  MembershipRepositories,
  MembershipTransactions,
} from '../domain/membership-transactions.js';
import { KnexClassBookingRepository } from './class-booking-store.js';
import { KnexClassSessionRepository } from './class-session-store.js';
import { KnexMemberRepository } from './member-store.js';

/** Membership's repositories bound to one knex transaction. */
export class KnexMembershipTransactions implements MembershipTransactions {
  constructor(
    private readonly db: Knex,
    private readonly cipher: FieldCipher,
  ) {}

  run<T>(work: (repositories: MembershipRepositories) => Promise<T>): Promise<T> {
    return this.db.transaction((tx) => work(this.repositoriesFor(tx)));
  }

  private repositoriesFor(tx: Knex.Transaction): MembershipRepositories {
    const members = new KnexMemberRepository(tx, this.cipher, new AuditLog(tx), new Outbox(tx));
    return {
      members,
      sessions: new KnexClassSessionRepository(tx),
      bookings: new KnexClassBookingRepository(tx, members),
    };
  }
}
