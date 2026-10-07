/** Where rendered labels are kept. */
export interface LabelStore {
  save(labelId: string, zpl: string): Promise<string>;
  load(labelId: string): Promise<string | undefined>;
  purgeOlderThan(cutoff: Date): Promise<number>;
}
