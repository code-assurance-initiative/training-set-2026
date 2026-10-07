import { describe, expect, it } from 'vitest';
import { CarrierSignatureVerifier } from '../../src/webhooks/partner-signature.js';
import { createCarrierSigner } from '../support/carrier-keys.js';

const carrier = createCarrierSigner();
const verifier = new CarrierSignatureVerifier(carrier.publicKey);
const body = '<statusFeed/>';
const bytes = new TextEncoder().encode(body);

describe('carrier signature', () => {
  it('accepts the carrier signature over the exact body', () => {
    expect(verifier.verify(bytes, carrier.sign(body))).toBe(true);
  });

  it('rejects a changed body', () => {
    expect(verifier.verify(new TextEncoder().encode('<statusFeed />'), carrier.sign(body))).toBe(
      false,
    );
  });

  it('rejects a signature by another key', () => {
    expect(verifier.verify(bytes, createCarrierSigner().sign(body))).toBe(false);
  });

  it.each([undefined, '', 'not base64!', Buffer.from('short').toString('base64')])(
    'rejects the signature %j',
    (signature) => {
      expect(verifier.verify(bytes, signature)).toBe(false);
    },
  );

  it('needs a 32-byte public key', () => {
    expect(() => new CarrierSignatureVerifier(new Uint8Array(16))).toThrow(RangeError);
  });
});
