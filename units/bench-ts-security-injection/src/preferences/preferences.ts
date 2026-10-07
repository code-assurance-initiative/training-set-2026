import type { Collection } from 'mongodb';
import { z } from 'zod';

export const preferencesShape = z.strictObject({
  search: z.strictObject({
    pageSize: z.number().int().min(10).max(100),
    defaultSort: z.enum(['newest', 'oldest', 'title', 'collection']),
    collections: z.array(z.string().min(1).max(40)).max(20),
  }),
  notifications: z.strictObject({
    exportReady: z.boolean(),
    shareOpened: z.boolean(),
  }),
});

export type Preferences = z.infer<typeof preferencesShape>;

export const defaultPreferences: Preferences = {
  search: { pageSize: 25, defaultSort: 'newest', collections: [] },
  notifications: { exportReady: true, shareOpened: false },
};

export interface PreferenceDocument {
  readonly _id: string;
  readonly preferences: Preferences;
  readonly updatedAt: Date;
}

export type PreferenceCollection = Pick<Collection<PreferenceDocument>, 'findOne' | 'updateOne'>;
