import { createHmac, timingSafeEqual } from 'node:crypto';

export interface CarrierCredentials {
  readonly carrier: string;
  readonly accountNumber: string;
  readonly apiKey: string;
}

export interface AuditEntry {
  readonly at: Date;
  readonly actor: string;
  readonly action: string;
}

/** Carrier accounts: credentials, rate limiting, audit trail and webhook signatures. */
export class CarrierAccountManager {
  private credentials = new Map<string, CarrierCredentials>();
  private rotatedAt = new Map<string, Date>();
  private m_windowStart = 0;
  private requestsInWindow = 0;
  private readonly maxPerMinute: number;
  private auditLog: AuditEntry[] = [];
  private auditLimit = 1000;
  private webhookSecret: Buffer;
  private secretVersion = 1;

  constructor(webhookSecret: Buffer, maxPerMinute = 120) {
    this.webhookSecret = webhookSecret;
    this.maxPerMinute = maxPerMinute;
  }

  // credentials

  setCredentials(credentials: CarrierCredentials, at: Date): void {
    this.credentials.set(credentials.carrier, credentials);
    this.rotatedAt.set(credentials.carrier, at);
  }

  credentialsFor(carrier: string): CarrierCredentials {
    const found = this.credentials.get(carrier);
    if (!found) {
      throw new Error(`No credentials for ${carrier}`);
    }
    return found;
  }

  needsRotation(carrier: string, now: Date, maxAgeDays = 90): boolean {
    const rotated = this.rotatedAt.get(carrier);
    return rotated === undefined || now.getTime() - rotated.getTime() > maxAgeDays * 86_400_000;
  }

  // rate limit

  tryAcquire(nowMs: number): boolean {
    if (nowMs - this.m_windowStart >= 60_000) {
      this.m_windowStart = nowMs;
      this.requestsInWindow = 0;
    }
    if (this.requestsInWindow >= this.maxPerMinute) {
      return false;
    }
    this.requestsInWindow += 1;
    return true;
  }

  remaining(nowMs: number): number {
    const inWindow = nowMs - this.m_windowStart < 60_000 ? this.requestsInWindow : 0;
    return Math.max(0, this.maxPerMinute - inWindow);
  }

  // audit

  audit(actor: string, action: string, at: Date): void {
    this.auditLog.push({ at, actor, action });
    if (this.auditLog.length > this.auditLimit) {
      this.auditLog.splice(0, this.auditLog.length - this.auditLimit);
    }
  }

  auditTrail(actor?: string): readonly AuditEntry[] {
    const entries = actor ? this.auditLog.filter((entry) => entry.actor === actor) : this.auditLog;
    return [...entries];
  }

  // webhooks

  signWebhook(payload: string): string {
    const mac = createHmac('sha256', this.webhookSecret).update(payload).digest('hex');
    return `v${String(this.secretVersion)}=${mac}`;
  }

  verifyWebhook(payload: string, signature: string): boolean {
    const expected = Buffer.from(this.signWebhook(payload));
    const actual = Buffer.from(signature);
    return expected.length === actual.length && timingSafeEqual(expected, actual);
  }

  rotateWebhookSecret(secret: Buffer): void {
    this.webhookSecret = secret;
    this.secretVersion += 1;
  }
}
