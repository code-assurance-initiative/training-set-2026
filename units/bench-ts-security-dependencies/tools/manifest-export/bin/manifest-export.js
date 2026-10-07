#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const parseArgs = require('minimist');
const { toCsv, toManifestRows } = require('../lib/manifest');

const usage =
  'usage: manifest-export --input <run.json> [--output <manifest.csv>] [--delimiter ";"]';

function main(argv) {
  const args = parseArgs(argv, {
    string: ['input', 'output', 'delimiter'],
    boolean: ['help'],
    alias: { i: 'input', o: 'output', d: 'delimiter', h: 'help' },
    default: { delimiter: ';' },
  });
  if (args.help) {
    console.log(usage);
    return 0;
  }
  if (!args.input) {
    console.error(usage);
    return 2;
  }
  const run = JSON.parse(fs.readFileSync(args.input, 'utf8'));
  const csv = toCsv(toManifestRows(run), args.delimiter);
  const output =
    args.output ||
    path.join(path.dirname(args.input), `manifest-${run.serviceDate}-${run.depotId}.csv`);
  fs.writeFileSync(output, csv, 'utf8');
  console.log(`${output}: ${csv.split('\r\n').length - 2} stops`);
  return 0;
}

if (require.main === module) {
  try {
    process.exitCode = main(process.argv.slice(2));
  } catch (error) {
    console.error(`manifest-export: ${error.message}`);
    process.exitCode = 1;
  }
}

module.exports = { main };
