/** Identifies the stock of one SKU held in one bin. */
export interface StockKey {
  readonly skuCode: string;
  readonly binCode: string;
}

export interface StockLevel extends StockKey {
  readonly onHand: number;
  readonly reserved: number;
  readonly available: number;
}
