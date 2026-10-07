/** Matches documents by field equality, which is all the routes under test ask for. */
function matches(document: Record<string, unknown>, filter: Record<string, unknown>): boolean {
  return Object.entries(filter).every(([key, value]) => document[key] === value);
}

/** A tiny in-memory stand-in for the MongoDB collection methods the service uses. */
export class FakeCollection<T extends { _id: string }> {
  readonly documents: T[] = [];

  constructor(initial: readonly T[] = []) {
    this.documents.push(...initial);
  }

  findOne(filter: Record<string, unknown>): Promise<T | null> {
    return Promise.resolve(this.documents.find((doc) => matches(doc, filter)) ?? null);
  }

  find(filter: Record<string, unknown>, options?: { projection?: Record<string, 0> }) {
    const found = this.documents.filter((doc) => matches(doc, filter));
    const hidden = Object.keys(options?.projection ?? {});
    return {
      toArray: () =>
        Promise.resolve(
          found.map((doc) =>
            Object.fromEntries(Object.entries(doc).filter(([key]) => !hidden.includes(key))),
          ) as T[],
        ),
    };
  }

  insertOne(document: T) {
    this.documents.push(document);
    return Promise.resolve({ acknowledged: true, insertedId: document._id });
  }

  deleteOne(filter: Record<string, unknown>) {
    const index = this.documents.findIndex((doc) => matches(doc, filter));
    if (index >= 0) {
      this.documents.splice(index, 1);
    }
    return Promise.resolve({ acknowledged: true, deletedCount: index >= 0 ? 1 : 0 });
  }

  updateOne(
    filter: Record<string, unknown>,
    update: { $set: Partial<T> },
    options?: { upsert?: boolean },
  ) {
    const existing = this.documents.find((doc) => matches(doc, filter));
    if (existing) {
      Object.assign(existing, update.$set);
    } else if (options?.upsert) {
      this.documents.push({ ...filter, ...update.$set } as T);
    }
    return Promise.resolve({ acknowledged: true, matchedCount: existing ? 1 : 0 });
  }
}
