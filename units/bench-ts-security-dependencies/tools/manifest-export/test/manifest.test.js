'use strict';

const assert = require('assert');
const fs = require('fs');
const os = require('os');
const path = require('path');
const test = require('node:test');
const { main } = require('../bin/manifest-export');
const { toCsv, toManifestRows } = require('../lib/manifest');

const stop = (trackingNumber, recipient, city) => ({
  trackingNumber,
  recipient,
  street: 'Søndergade 1',
  postcode: '8000',
  city,
  weightKg: 2.5,
  window: { from: '2026-11-02 08:00', to: '2026-11-02 10:00' },
});

const run = {
  depotId: 'AAR',
  serviceDate: '2026-11-02',
  routes: [
    { vehicle: 1, trackingNumbers: ['AB00000003', 'AB00000001'] },
    { vehicle: 2, trackingNumbers: ['AB00000002'] },
  ],
  stops: [
    stop('AB00000001', 'Hansen; Jensen', 'Aarhus C'),
    stop('AB00000002', 'Ole "Bager" Nielsen', 'Viby J'),
    stop('AB00000003', 'Lund', 'Aarhus C'),
  ],
};

test('rows follow the loading order of each vehicle', () => {
  const rows = toManifestRows(run);
  assert.deepStrictEqual(
    rows.map((row) => [row.vehicle, row.position, row.tracking_number]),
    [
      [1, 1, 'AB00000003'],
      [1, 2, 'AB00000001'],
      [2, 1, 'AB00000002'],
    ],
  );
  assert.strictEqual(rows[0].weight_kg, '2.50');
});

test('a route that names a missing stop is refused', () => {
  assert.throws(() => toManifestRows({ ...run, stops: [] }), /has no stop/);
  assert.throws(() => toManifestRows({}), /Not a dispatch run export/);
});

test('fields with the delimiter or quotes are quoted; lines end with CRLF', () => {
  const csv = toCsv(toManifestRows(run));
  const lines = csv.split('\r\n');
  assert.strictEqual(
    lines[0],
    'vehicle;position;tracking_number;recipient;street;postcode;city;window_from;window_to;weight_kg',
  );
  assert.strictEqual(
    lines[2],
    '1;2;AB00000001;"Hansen; Jensen";Søndergade 1;8000;Aarhus C;2026-11-02 08:00;2026-11-02 10:00;2.50',
  );
  assert.strictEqual(lines[3].split(';')[3], '"Ole ""Bager"" Nielsen"');
  assert.strictEqual(lines[4], '');
});

test('the delimiter is one safe character', () => {
  assert.strictEqual(toCsv([], ',').trimEnd().includes(','), true);
  assert.throws(() => toCsv([], ';;'), RangeError);
  assert.throws(() => toCsv([], '"'), RangeError);
});

test('the command writes the manifest next to the export', () => {
  const directory = fs.mkdtempSync(path.join(os.tmpdir(), 'manifest-export-'));
  try {
    const input = path.join(directory, 'run.json');
    fs.writeFileSync(input, JSON.stringify(run));
    assert.strictEqual(main(['--input', input, '-d', ',']), 0);
    const csv = fs.readFileSync(path.join(directory, 'manifest-2026-11-02-AAR.csv'), 'utf8');
    assert.strictEqual(csv.split('\r\n').length, 5);
    assert.strictEqual(main([]), 2);
    assert.strictEqual(main(['--help']), 0);
  } finally {
    fs.rmSync(directory, { recursive: true, force: true });
  }
});
