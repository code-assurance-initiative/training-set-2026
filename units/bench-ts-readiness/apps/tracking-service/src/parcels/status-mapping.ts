import type { ParcelStatus } from "./parcel.js";

const inTransitCodes = new Set(["AR", "DP", "TR", "HB", "EXR"]);

/**
 * Maps a carrier event code to a parcel status. Codes are a two-letter family with an optional detail letter:
 *
 * - `DL` delivered, `DLV` delivered to a safe place, `DLX` delivery attempt failed;
 * - `EX` exception (damaged, address problem, customs hold …), `EXR` exception resolved;
 * - `OD` out for delivery, `RT` returned to sender;
 * - `AR` arrived at a facility, `DP` departed, `TR` in transit, `HB` handed to the line-haul partner.
 *
 * Unknown codes return `undefined` and leave the parcel's status unchanged.
 */
export function statusForCarrierCode(rawCode: string): ParcelStatus | undefined {
  const code = rawCode.trim().toUpperCase();
  if (code.startsWith("DL") || code.startsWith("DLV")) {
    return "delivered";
  }
  if (code.startsWith("OD")) {
    return "out_for_delivery";
  }
  if (code.startsWith("RT")) {
    return "returned";
  }
  if (code.startsWith("EX") && !code.startsWith("EXR")) {
    return "exception";
  }
  return inTransitCodes.has(code) ? "in_transit" : undefined;
}
