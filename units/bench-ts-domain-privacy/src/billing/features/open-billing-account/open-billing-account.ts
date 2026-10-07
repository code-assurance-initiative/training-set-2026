import type { IntegrationMessage } from '../../../platform/integration-bus.js';
import { memberRegisteredSchema } from '../../../membership/contracts/integration-events.js';
import { MemberId } from '../../../membership/domain/members/member-id.js';
import { BillingAccount } from '../../domain/accounts/billing-account.js';
import { BillingAccountId } from '../../domain/accounts/billing-account-id.js';
import { HolderBirthDate } from '../../domain/accounts/concession.js';
import type { BillingTransactions } from '../../domain/billing-transactions.js';

/** Opens the billing account of a newly registered member. A redelivered message changes nothing. */
export class OpenBillingAccountHandler {
  constructor(private readonly transactions: BillingTransactions) {}

  async handle(message: IntegrationMessage): Promise<void> {
    const payload = memberRegisteredSchema.parse(message.payload);
    const memberId = MemberId.of(payload.memberId);
    await this.transactions.run(async ({ accounts }) => {
      if (await accounts.findByMember(memberId)) {
        return;
      }
      const account = new BillingAccount(
        BillingAccountId.create(),
        memberId,
        {
          name: `${payload.firstName} ${payload.lastName}`,
          email: payload.email,
          birthDate: HolderBirthDate.parse(payload.dateOfBirth),
        },
        new Date(message.occurredAt),
      );
      await accounts.save(account);
    });
  }
}
