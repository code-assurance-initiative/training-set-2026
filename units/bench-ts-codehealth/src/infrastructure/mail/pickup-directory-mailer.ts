import { randomUUID } from 'node:crypto';
import { mkdir, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import type { Mailer } from '../../domain/ports/mailer.js';

/**
 * Drops each message as an .eml file into the pickup directory of the local mail transfer agent,
 * which delivers it (ADR 0002: the service holds no SMTP credentials).
 */
export class PickupDirectoryMailer implements Mailer {
  constructor(private readonly directory: string) {}

  async send(to: string, message: string): Promise<void> {
    await mkdir(this.directory, { recursive: true });
    const envelope = `X-Envelope-To: ${to}\r\n${message}`;
    await writeFile(join(this.directory, `${randomUUID()}.eml`), envelope, 'utf8');
  }
}
