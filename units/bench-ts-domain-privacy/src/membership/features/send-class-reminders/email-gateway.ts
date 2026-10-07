import type { Logger } from 'pino';
import { postToProvider, type ProviderEndpoint } from '../../../platform/provider-client.js';
import type { Pseudonymiser } from '../../../platform/pseudonym.js';
import type { EmailAddress } from '../../domain/members/email-address.js';
import type { MemberId } from '../../domain/members/member-id.js';

export interface EmailMessage {
  readonly memberId: MemberId;
  readonly to: EmailAddress;
  readonly subject: string;
  readonly text: string;
}

export interface EmailGateway {
  /** Sends one e-mail; resolves to false when the provider did not accept it. */
  send(message: EmailMessage): Promise<boolean>;
}

/** The transactional e-mail provider's REST API. */
export class HttpEmailGateway implements EmailGateway {
  constructor(
    private readonly provider: ProviderEndpoint,
    private readonly pseudonymise: Pseudonymiser,
    private readonly logger: Logger,
    private readonly fetchFn: typeof fetch = fetch,
  ) {}

  async send(message: EmailMessage): Promise<boolean> {
    const { to, subject, text } = message;
    const { accepted, status } = await postToProvider(
      this.provider,
      { to: to.value, subject, text },
      this.fetchFn,
    );
    if (!accepted) {
      this.logger.warn(
        { member: this.pseudonymise(message.memberId.value), domain: to.domain, status },
        'E-mail reminder was rejected by the provider',
      );
    }
    return accepted;
  }
}
