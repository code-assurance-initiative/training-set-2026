import type { Logger } from 'pino';
import { postToProvider, type ProviderEndpoint } from '../../../platform/provider-client.js';
import type { PhoneNumber } from '../../domain/members/phone-number.js';

export interface SmsGateway {
  /** Sends one text message; resolves to false when the provider did not accept it. */
  send(phoneNumber: PhoneNumber, text: string): Promise<boolean>;
}

/** The SMS provider's REST API. */
export class HttpSmsGateway implements SmsGateway {
  constructor(
    private readonly provider: ProviderEndpoint,
    private readonly logger: Logger,
    private readonly fetchFn: typeof fetch = fetch,
  ) {}

  async send(phoneNumber: PhoneNumber, text: string): Promise<boolean> {
    const { accepted, status } = await postToProvider(
      this.provider,
      { to: phoneNumber.value, text },
      this.fetchFn,
    );
    if (!accepted) {
      this.logger.warn(
        { phoneNumber: phoneNumber.value, status },
        'SMS reminder was rejected by the provider',
      );
    }
    return accepted;
  }
}
