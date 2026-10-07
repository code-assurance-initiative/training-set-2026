import type { Logger } from 'pino';
import type { CrmClient } from './crm-client.js';
import type { MailTransport } from './mail-transport.js';
import { pseudonymize } from './pseudonym.js';
import type { Subscription } from './subscription.js';

export interface DeliverableReport {
  readonly id: string;
  readonly name: string;
  readonly link: string;
}

export interface DeliverySummary {
  readonly delivered: number;
  readonly bounced: number;
}

/** Mails a report link to each subscriber, greeting them by the name the CRM knows them by. */
export class ReportMailer {
  constructor(
    private readonly transport: MailTransport,
    private readonly crm: Pick<CrmClient, 'findContact'>,
    private readonly pseudonymKey: string,
    private readonly publicBaseUrl: string,
    private readonly logger: Logger,
  ) {}

  async deliver(
    report: DeliverableReport,
    subscriptions: readonly Subscription[],
  ): Promise<DeliverySummary> {
    let delivered = 0;
    let bounced = 0;
    for (const subscription of subscriptions) {
      const contact = await this.crm.findContact(subscription.email);
      const outcome = await this.transport.send({
        to: subscription.email,
        subject: `Report: ${report.name}`,
        text: [
          `Hello ${contact?.displayName ?? 'there'},`,
          '',
          `The report "${report.name}" is ready: ${report.link}`,
          '',
          `Unsubscribe: ${this.publicBaseUrl}/unsubscribe?token=${subscription.unsubscribeToken}`,
        ].join('\n'),
      });
      if (outcome === 'bounced') {
        bounced += 1;
        this.recordBounce(report, subscription);
        continue;
      }
      delivered += 1;
      this.logger.info({ reportId: report.id, email: subscription.email }, 'Report delivered');
    }
    return { delivered, bounced };
  }

  /** Bounces are logged under a pseudonym: operations needs to see repeats, not the address. */
  private recordBounce(report: DeliverableReport, subscription: Subscription): void {
    this.logger.warn(
      { reportId: report.id, recipientRef: pseudonymize(subscription.email, this.pseudonymKey) },
      'Report delivery bounced',
    );
  }
}
