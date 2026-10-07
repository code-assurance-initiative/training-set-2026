import { randomBytes } from 'node:crypto';

interface Grant {
  readonly token: string;
  readonly expiresAt: number;
}

/**
 * Short-lived proofs that a browser unlocked a share link with its password. Kept in memory: a
 * restart only asks the reader for the password again.
 */
export class ShareGrants {
  private readonly grants = new Map<string, Grant>();

  constructor(
    private readonly lifetimeMs = 15 * 60_000,
    private readonly now: () => number = Date.now,
  ) {}

  issue(token: string): string {
    this.sweep();
    const id = randomBytes(24).toString('base64url');
    this.grants.set(id, { token, expiresAt: this.now() + this.lifetimeMs });
    return id;
  }

  allows(grantId: string | undefined, token: string): boolean {
    const grant = grantId === undefined ? undefined : this.grants.get(grantId);
    return grant?.token === token && grant.expiresAt > this.now();
  }

  private sweep(): void {
    const now = this.now();
    for (const [id, grant] of this.grants) {
      if (grant.expiresAt <= now) {
        this.grants.delete(id);
      }
    }
  }
}
