import { Router } from 'express';
import { sendProblem } from '../http/problem.js';
import { callerOf } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import { deepMerge, isPlainObject } from './deep-merge.js';
import { defaultPreferences, preferencesShape, type PreferenceCollection } from './preferences.js';

/** Each signed-in user's own search and notification preferences. */
export function preferenceRoutes(collection: PreferenceCollection, now: () => Date) {
  async function current(subject: string) {
    const stored = await collection.findOne({ _id: subject });
    return stored?.preferences ?? defaultPreferences;
  }

  return Router()
    .get('/preferences', async (_req, res) => {
      res.json(await current(callerOf(res)));
    })
    .patch('/preferences', async (req, res) => {
      if (!isPlainObject(req.body)) {
        sendProblem(res, 400, 'Send a JSON object with the preferences to change.');
        return;
      }
      const subject = callerOf(res);
      const merged = deepMerge(structuredClone(await current(subject)), req.body);
      const preferences = parseInput(preferencesShape, merged, res);
      if (!preferences) {
        return;
      }
      await collection.updateOne(
        { _id: subject },
        { $set: { preferences, updatedAt: now() } },
        { upsert: true },
      );
      res.json(preferences);
    });
}
