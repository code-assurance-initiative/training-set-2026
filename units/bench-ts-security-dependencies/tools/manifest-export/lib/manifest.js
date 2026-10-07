'use strict';

const COLUMNS = [
  'vehicle',
  'position',
  'tracking_number',
  'recipient',
  'street',
  'postcode',
  'city',
  'window_from',
  'window_to',
  'weight_kg',
];

/**
 * One manifest row per stop, in loading order: vehicle by vehicle, stops in the order the route lists them.
 * @param {{ routes: Array<{ vehicle: number, trackingNumbers: string[] }>, stops: Array<object> }} run
 */
function toManifestRows(run) {
  if (!run || !Array.isArray(run.routes) || !Array.isArray(run.stops)) {
    throw new TypeError('Not a dispatch run export: routes and stops are required.');
  }
  const stopsByTrackingNumber = new Map(run.stops.map((stop) => [stop.trackingNumber, stop]));
  const rows = [];
  for (const route of run.routes) {
    route.trackingNumbers.forEach((trackingNumber, index) => {
      const stop = stopsByTrackingNumber.get(trackingNumber);
      if (!stop) {
        throw new TypeError(`Route ${route.vehicle} lists ${trackingNumber}, which has no stop.`);
      }
      rows.push({
        vehicle: route.vehicle,
        position: index + 1,
        tracking_number: trackingNumber,
        recipient: stop.recipient,
        street: stop.street,
        postcode: stop.postcode,
        city: stop.city,
        window_from: stop.window.from,
        window_to: stop.window.to,
        weight_kg: stop.weightKg.toFixed(2),
      });
    });
  }
  return rows;
}

/** RFC 4180 quoting: a field with the delimiter, a quote or a line break is quoted, quotes doubled. */
function quote(value, delimiter) {
  const text = String(value);
  return text.includes(delimiter) || /["\r\n]/.test(text) ? `"${text.replace(/"/g, '""')}"` : text;
}

/** The manifest as CSV with a header line; the depot printers expect CRLF line ends. */
function toCsv(rows, delimiter = ';') {
  if (delimiter.length !== 1 || /["\r\n]/.test(delimiter)) {
    throw new RangeError('The delimiter must be one character other than a quote or a line break.');
  }
  const lines = [COLUMNS.join(delimiter)];
  for (const row of rows) {
    lines.push(COLUMNS.map((column) => quote(row[column], delimiter)).join(delimiter));
  }
  return `${lines.join('\r\n')}\r\n`;
}

module.exports = { COLUMNS, toManifestRows, toCsv };
