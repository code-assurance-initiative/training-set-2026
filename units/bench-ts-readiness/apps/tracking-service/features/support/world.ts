import { After, Before, setWorldConstructor, World } from "@cucumber/cucumber";
import type { Knex } from "knex";
import { ParcelStore } from "../../src/parcels/parcel-store.js";
import { TrackingService, type ParcelWithEvents } from "../../src/parcels/tracking-service.js";
import { createTestDatabase } from "../../tests/support/database.js";

/** One fresh database and service per scenario. */
export class TrackingWorld extends World {
  db!: Knex;
  store!: ParcelStore;
  service!: TrackingService;
  now = new Date("2026-10-05T08:00:00Z");
  lookup: ParcelWithEvents | Error | undefined;
}

setWorldConstructor(TrackingWorld);

Before(async function (this: TrackingWorld) {
  this.db = await createTestDatabase();
  this.store = new ParcelStore(this.db);
  this.service = new TrackingService(this.store, () => this.now);
});

After(async function (this: TrackingWorld) {
  await this.db.destroy();
});
