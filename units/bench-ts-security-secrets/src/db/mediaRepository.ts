import type pg from "pg";

export interface MediaRecord {
  readonly id: string;
  readonly ownerId: string;
  readonly objectKey: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly createdAt: Date;
}

export interface OwnerUsage {
  readonly ownerId: string;
  readonly stripeCustomerId: string;
  readonly storedBytes: number;
  readonly uploadsOnDay: number;
}

export interface MediaRepository {
  insert(record: MediaRecord): Promise<void>;
  findById(id: string): Promise<MediaRecord | undefined>;
  totalBytesForOwner(ownerId: string): Promise<number>;
  usageOn(day: string): Promise<OwnerUsage[]>;
}

interface MediaRow {
  id: string;
  owner_id: string;
  object_key: string;
  content_type: string;
  size_bytes: string;
  created_at: Date;
}

export class PostgresMediaRepository implements MediaRepository {
  constructor(private readonly pool: pg.Pool) {}

  async insert(record: MediaRecord): Promise<void> {
    await this.pool.query(
      `insert into media (id, owner_id, object_key, content_type, size_bytes, created_at)
       values ($1, $2, $3, $4, $5, $6)`,
      [record.id, record.ownerId, record.objectKey, record.contentType, record.sizeBytes, record.createdAt],
    );
  }

  async findById(id: string): Promise<MediaRecord | undefined> {
    const result = await this.pool.query<MediaRow>(
      `select id, owner_id, object_key, content_type, size_bytes, created_at from media where id = $1`,
      [id],
    );
    const row = result.rows[0];
    return row === undefined ? undefined : toRecord(row);
  }

  async totalBytesForOwner(ownerId: string): Promise<number> {
    const result = await this.pool.query<{ total: string | null }>(
      `select sum(size_bytes)::text as total from media where owner_id = $1`,
      [ownerId],
    );
    return Number(result.rows[0]?.total ?? 0);
  }

  async usageOn(day: string): Promise<OwnerUsage[]> {
    const result = await this.pool.query<{
      owner_id: string;
      stripe_customer_id: string;
      stored_bytes: string;
      uploads_on_day: string;
    }>(
      `select o.id as owner_id,
              o.stripe_customer_id,
              coalesce(sum(m.size_bytes), 0)::text as stored_bytes,
              count(m.id) filter (where m.created_at::date = $1::date)::text as uploads_on_day
         from owners o
         left join media m on m.owner_id = o.id and m.created_at::date <= $1::date
        group by o.id, o.stripe_customer_id`,
      [day],
    );
    return result.rows.map((row) => ({
      ownerId: row.owner_id,
      stripeCustomerId: row.stripe_customer_id,
      storedBytes: Number(row.stored_bytes),
      uploadsOnDay: Number(row.uploads_on_day),
    }));
  }
}

function toRecord(row: MediaRow): MediaRecord {
  return {
    id: row.id,
    ownerId: row.owner_id,
    objectKey: row.object_key,
    contentType: row.content_type,
    sizeBytes: Number(row.size_bytes),
    createdAt: row.created_at,
  };
}
