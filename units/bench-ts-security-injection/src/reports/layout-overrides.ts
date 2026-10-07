import { z } from 'zod';

export const layoutSettingsShape = z.strictObject({
  orientation: z.enum(['portrait', 'landscape']),
  pageSize: z.enum(['A4', 'A3', 'Letter']),
  fontSize: z.number().int().min(6).max(24),
  showFooter: z.boolean(),
});

export type LayoutSettings = z.infer<typeof layoutSettingsShape>;

export const defaultLayoutSettings: LayoutSettings = {
  orientation: 'portrait',
  pageSize: 'A4',
  fontSize: 10,
  showFooter: true,
};

const reservedKeys = new Set(['__proto__', 'constructor', 'prototype']);
const settingKeys = new Set(Object.keys(defaultLayoutSettings));

/** Applies the caller's per-preview overrides to the layout settings; unknown keys are ignored. */
export function applyLayoutOverrides(base: LayoutSettings, overrides: unknown): LayoutSettings {
  const merged = Object.assign(Object.create(null) as Record<string, unknown>, base);
  if (typeof overrides === 'object' && overrides !== null && !Array.isArray(overrides)) {
    for (const [key, value] of Object.entries(overrides)) {
      if (reservedKeys.has(key) || !settingKeys.has(key)) {
        continue;
      }
      merged[key] = value;
    }
  }
  return layoutSettingsShape.parse(merged);
}
