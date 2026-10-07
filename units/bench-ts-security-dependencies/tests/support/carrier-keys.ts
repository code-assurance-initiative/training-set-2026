import nacl from 'tweetnacl';

export interface CarrierSigner {
  readonly publicKey: Uint8Array;
  sign(body: string): string;
}

/** An Ed25519 key pair standing in for the carrier's, generated for this test run only. */
export function createCarrierSigner(): CarrierSigner {
  const keys = nacl.sign.keyPair();
  return {
    publicKey: keys.publicKey,
    sign: (body) =>
      Buffer.from(nacl.sign.detached(new TextEncoder().encode(body), keys.secretKey)).toString(
        'base64',
      ),
  };
}
