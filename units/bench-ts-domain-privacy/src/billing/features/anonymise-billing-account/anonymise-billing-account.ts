import type { IntegrationMessage } from '../../../platform/integration-bus.js';
import { memberErasedSchema } from '../../../membership/contracts/integration-events.js';
import { MemberId } from '../../../membership/domain/members/member-id.js';
import type { BillingTransactions } from '../../domain/billing-transactions.js';

/** Removes the holder's personal data from Billing when Membership erases the member. */
export class AnonymiseBillingAccountHandler {
  constructor(private readonly transactions: BillingTransactions) {}

  async handle(message: IntegrationMessage): Promise<void> {
    const payload = memberErasedSchema.parse(message.payload);
    await this.transactions.run(async ({ accounts }) => {
      const account = await accounts.findByMember(MemberId.of(payload.memberId));
      if (account) {
        account.anonymise(new Date(message.occurredAt));
        await accounts.save(account);
      }
    });
  }
}
