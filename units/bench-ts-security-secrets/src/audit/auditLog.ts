import { MongoClient, type Collection } from "mongodb";

export interface AuditEvent {
  readonly action: "media.uploaded" | "media.download-token-issued" | "media.downloaded";
  readonly mediaId: string;
  readonly actor: string;
  readonly at: Date;
}

export interface AuditLog {
  record(event: AuditEvent): Promise<void>;
  close(): Promise<void>;
}

export class MongoAuditLog implements AuditLog {
  private constructor(
    private readonly client: MongoClient,
    private readonly events: Collection<AuditEvent>,
  ) {}

  static async connect(uri: string): Promise<MongoAuditLog> {
    const client = new MongoClient(uri, { appName: "media-intake", retryWrites: true });
    await client.connect();
    return new MongoAuditLog(client, client.db("audit").collection<AuditEvent>("media_events"));
  }

  async record(event: AuditEvent): Promise<void> {
    await this.events.insertOne({ ...event });
  }

  async close(): Promise<void> {
    await this.client.close();
  }
}

/** Used when no audit cluster is configured: events go to the structured log only. */
export class LogOnlyAuditLog implements AuditLog {
  constructor(private readonly write: (event: AuditEvent) => void) {}

  record(event: AuditEvent): Promise<void> {
    this.write(event);
    return Promise.resolve();
  }

  close(): Promise<void> {
    return Promise.resolve();
  }
}
