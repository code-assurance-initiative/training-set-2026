import { describe, expect, it } from "vitest";
import { statusForCarrierCode } from "../../src/parcels/status-mapping.js";

describe("carrier code mapping", () => {
  it.each([
    ["DL", "delivered"],
    ["DLV", "delivered"],
    ["EX", "exception"],
    ["EXD", "exception"],
    ["EXR", "in_transit"],
    ["OD", "out_for_delivery"],
    ["RT", "returned"],
    ["AR", "in_transit"],
    ["DP", "in_transit"],
    ["TR", "in_transit"],
    ["HB", "in_transit"],
  ] as const)("maps %s to %s", (code, status) => {
    expect(statusForCarrierCode(code)).toBe(status);
  });

  it("normalises case and surrounding whitespace", () => {
    expect(statusForCarrierCode(" od ")).toBe("out_for_delivery");
  });

  it("leaves unknown codes unmapped", () => {
    expect(statusForCarrierCode("ZZ")).toBeUndefined();
    expect(statusForCarrierCode("")).toBeUndefined();
  });
});
