export interface SignatureHeader {
  timestamp: number;
  signatures: string[];
}

/** Parses `t=<unix seconds>,v1=<hex>[,v1=<hex>…]`; several v1 entries appear while the sender rotates its secret. */
export function parseSignatureHeader(header: string): SignatureHeader | undefined {
  let timestamp: number | undefined;
  const signatures: string[] = [];
  for (const part of header.split(",")) {
    const [key, value] = part.trim().split("=", 2);
    if (key === "t" && value !== undefined && /^\d{1,12}$/.test(value)) {
      timestamp = Number(value);
    } else if (key === "v1" && value !== undefined && /^[0-9a-f]{64}$/.test(value)) {
      signatures.push(value);
    }
  }
  return timestamp === undefined || signatures.length === 0 ? undefined : { timestamp, signatures };
}
