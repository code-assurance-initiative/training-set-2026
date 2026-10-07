import { mkdtemp, readdir, utimes, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';
import { Shipment } from '../../src/domain/entities/shipment.js';
import { LabelArchive } from '../../src/infrastructure/labels/label-archive.js';
import { PickupDirectoryMailer } from '../../src/infrastructure/mail/pickup-directory-mailer.js';
import { InMemoryShipmentRepository } from '../../src/infrastructure/persistence/in-memory-shipment-repository.js';
import { aarhus, copenhagen, hamburg, parcel } from '../support/builders.js';
import { silentLogger } from '../support/silent-logger.js';

describe('LabelArchive', () => {
  it('saves, loads and purges label files', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'labels-'));
    const archive = new LabelArchive(join(directory, 'labels'), silentLogger);
    expect(await archive.purgeOlderThan(new Date())).toBe(0);
    const path = await archive.save('label-1', '^XA^XZ');
    expect(await archive.load('label-1')).toBe('^XA^XZ');
    expect(await archive.load('label-2')).toBeUndefined();
    await utimes(path, new Date('2026-01-01'), new Date('2026-01-01'));
    await archive.save('label-3', '^XA^XZ');
    expect(await archive.purgeOlderThan(new Date('2026-06-01'))).toBe(1);
    await expect(archive.load('../etc/passwd')).rejects.toThrow(RangeError);
  });

  it('logs and rethrows when the label cannot be written', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'labels-'));
    const blocker = join(directory, 'file');
    await writeFile(blocker, '');
    await expect(
      new LabelArchive(blocker, silentLogger).save('label-1', '^XA^XZ'),
    ).rejects.toThrow();
  });
});

describe('PickupDirectoryMailer', () => {
  it('drops one .eml file per message', async () => {
    const directory = join(await mkdtemp(join(tmpdir(), 'outbox-')), 'pickup');
    await new PickupDirectoryMailer(directory).send('a@example.test', 'Subject: hi\r\n\r\nhello');
    expect((await readdir(directory)).filter((name) => name.endsWith('.eml'))).toHaveLength(1);
  });
});

describe('InMemoryShipmentRepository', () => {
  const at = new Date('2026-10-07T10:00:00.000Z');
  const shipment = (id: string, carrier = 'alder', recipient = aarhus) =>
    new Shipment(id, carrier, 'standard', copenhagen, recipient, [parcel()], at);

  it('stores, finds, counts and purges shipments', () => {
    const repository = new InMemoryShipmentRepository();
    repository.add(shipment('s1'));
    repository.add(shipment('s2', 'corvid', hamburg));
    expect(() => {
      repository.add(shipment('s1'));
    }).toThrow(/already exists/);
    repository.markLabelled('s1', 'ALD0000000001', '/labels/s1.zpl');
    expect(repository.findByTrackingNumber('ALD0000000001')?.id).toBe('s1');
    expect(repository.findByRecipientPostcode('20099').map((s) => s.id)).toEqual(['s2']);
    expect(repository.listByCarrier('corvid')).toHaveLength(1);
    expect(repository.listByStatus('labelled')).toHaveLength(1);
    expect(repository.listCreatedBetween(at, new Date('2026-10-08'))).toHaveLength(2);
    expect(repository.listOpen()).toHaveLength(2);
    repository.markArchived('s1');
    repository.markVoided('s2');
    expect(repository.countByStatus()).toEqual({ draft: 0, labelled: 0, archived: 1, voided: 1 });
    expect(repository.countByCarrier()).toEqual({ alder: 1, corvid: 1 });
    expect(() => {
      repository.markArchived('s9');
    }).toThrow(/No shipment/);
    repository.update(shipment('s3'));
    expect(repository.remove('s3')).toBe(true);
    expect(repository.purgeCreatedBefore(new Date('2026-10-08'))).toBe(2);
    repository.add(shipment('s4'));
    repository.clear();
    expect(repository.findById('s4')).toBeUndefined();
  });
});
