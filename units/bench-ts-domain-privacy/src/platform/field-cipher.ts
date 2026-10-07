import { createCipheriv, createDecipheriv, randomBytes } from 'node:crypto';

const algorithm = 'aes-256-gcm';
const version = 'v1';
const ivBytes = 12;
const authTagLength = 16;

/** Encrypts single personal-data fields for storage (AES-256-GCM, a fresh IV per value; ADR 0004). */
export interface FieldCipher {
  encrypt(plaintext: string): string;
  decrypt(stored: string): string;
}

export function createFieldCipher(key: Buffer): FieldCipher {
  if (key.length !== 32) {
    throw new RangeError('The field-encryption key must be 256 bits.');
  }
  return {
    encrypt(plaintext) {
      const iv = randomBytes(ivBytes);
      const cipher = createCipheriv(algorithm, key, iv, { authTagLength });
      const ciphertext = Buffer.concat([cipher.update(plaintext, 'utf8'), cipher.final()]);
      return [version, iv, cipher.getAuthTag(), ciphertext].map(encodePart).join(':');
    },
    decrypt(stored) {
      const [prefix, iv, tag, ciphertext] = stored.split(':');
      if (prefix !== version || !iv || !tag || ciphertext === undefined) {
        throw new Error('Not a value written by this field cipher.');
      }
      const decipher = createDecipheriv(algorithm, key, Buffer.from(iv, 'base64url'), {
        authTagLength,
      });
      decipher.setAuthTag(Buffer.from(tag, 'base64url'));
      return Buffer.concat([
        decipher.update(Buffer.from(ciphertext, 'base64url')),
        decipher.final(),
      ]).toString('utf8');
    },
  };
}

function encodePart(part: string | Buffer): string {
  return typeof part === 'string' ? part : part.toString('base64url');
}
