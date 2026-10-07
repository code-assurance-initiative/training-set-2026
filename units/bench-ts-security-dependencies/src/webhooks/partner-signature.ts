import nacl from 'tweetnacl';

/** Verifies the carrier's detached Ed25519 signature over the exact bytes of a webhook body. */
export class CarrierSignatureVerifier {
  constructor(private readonly publicKey: Uint8Array) {
    if (publicKey.length !== nacl.sign.publicKeyLength) {
      throw new RangeError(`An Ed25519 public key has ${nacl.sign.publicKeyLength} bytes.`);
    }
  }

  /** True only for a well-formed base64 signature that the carrier's key verifies over `body`. */
  verify(body: Uint8Array, signatureBase64: string | undefined): boolean {
    if (signatureBase64 === undefined || !/^[A-Za-z0-9+/]+={0,2}$/.test(signatureBase64)) {
      return false;
    }
    const signature = new Uint8Array(Buffer.from(signatureBase64, 'base64'));
    if (signature.length !== nacl.sign.signatureLength) {
      return false;
    }
    return nacl.sign.detached.verify(body, signature, this.publicKey);
  }
}
