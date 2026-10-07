import type { Logger } from 'pino';
import { addUtcMonths, utcDay, type Clock } from '../../../platform/clock.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

const actor = 'job:purge-lapsed-members';

/**
 * Storage limitation: erases the personal data of members whose paid period ended more than the
 * retention period ago (docs/privacy/data-inventory.md).
 */
export class PurgeLapsedMembersHandler {
  constructor(
    private readonly transactions: MembershipTransactions,
    private readonly clock: Clock,
    private readonly retentionMonths: number,
    private readonly logger: Logger,
  ) {}

  async run(): Promise<number> {
    const now = this.clock.now();
    const cutoff = addUtcMonths(utcDay(now), -this.retentionMonths);
    const lapsed = await this.transactions.run(({ members }) => members.listLapsedBefore(cutoff));
    for (const candidate of lapsed) {
      await this.transactions.run(async ({ members }) => {
        const member = await members.get(candidate.id);
        if (member && member.status !== 'erased') {
          member.erase(now);
          await members.save(member, actor);
        }
      });
    }
    this.logger.info({ erased: lapsed.length, cutoff }, 'Lapsed members purged');
    return lapsed.length;
  }
}
