import type { MediaRecord, MediaRepository, OwnerUsage } from "../../src/db/mediaRepository.js";

export class InMemoryMediaRepository implements MediaRepository {
  readonly records = new Map<string, MediaRecord>();

  insert(record: MediaRecord): Promise<void> {
    this.records.set(record.id, record);
    return Promise.resolve();
  }

  findById(id: string): Promise<MediaRecord | undefined> {
    return Promise.resolve(this.records.get(id));
  }

  totalBytesForOwner(ownerId: string): Promise<number> {
    const records = [...this.records.values()].filter((record) => record.ownerId === ownerId);
    return Promise.resolve(records.reduce((sum, record) => sum + record.sizeBytes, 0));
  }

  usageOn(): Promise<OwnerUsage[]> {
    return Promise.resolve([]);
  }
}
