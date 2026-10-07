import { describe, expect, it } from 'vitest';
import { flattenMetadata } from '../../src/imports/metadata-flattener.js';
import { readMetadata } from '../../src/imports/metadata-importer.js';
import { readRetentionSchedule } from '../../src/imports/retention-schedule-reader.js';
import { InvalidXmlError } from '../../src/imports/xml-elements.js';

describe('metadata importer', () => {
  it('reads one field per child element and substitutes declared entities', () => {
    const xml = `<?xml version="1.0"?>
<!DOCTYPE metadata [<!ENTITY agency "Regional Records Office">]>
<metadata>
  <title>Minutes of the board</title>
  <creator>&agency;</creator>
  <!-- scanned at station 4 -->
  <pages> 12 </pages>
</metadata>`;

    expect(readMetadata(xml)).toEqual([
      { name: 'title', value: 'Minutes of the board' },
      { name: 'creator', value: 'Regional Records Office' },
      { name: 'pages', value: '12' },
    ]);
  });

  it('refuses another document type', () => {
    expect(() => readMetadata('<catalogue/>')).toThrow(InvalidXmlError);
  });

  it('refuses malformed XML', () => {
    expect(() => readMetadata('<metadata><title>')).toThrow(InvalidXmlError);
  });
});

describe('retention schedule reader', () => {
  it('reads one rule per series', () => {
    const xml =
      '<retention-schedule><rule series="HR" years="10"/><note/><rule series="FIN" years="7"/></retention-schedule>';

    expect(readRetentionSchedule(xml)).toEqual([
      { series: 'HR', years: 10 },
      { series: 'FIN', years: 7 },
    ]);
  });

  it('refuses a rule that references an external entity', () => {
    const xml = `<!DOCTYPE retention-schedule [<!ENTITY s SYSTEM "file:///etc/hostname">]>
<retention-schedule><rule series="&s;" years="10"/></retention-schedule>`;

    expect(() => readRetentionSchedule(xml)).toThrow(InvalidXmlError);
  });

  it.each([
    '<rule series="hr" years="10"/>',
    '<rule series="HR" years="0"/>',
    '<rule series="HR"/>',
  ])('refuses %s', (rule) => {
    expect(() => readRetentionSchedule(`<retention-schedule>${rule}</retention-schedule>`)).toThrow(
      /line 1/,
    );
  });

  it('refuses another document type', () => {
    expect(() => readRetentionSchedule('<metadata/>')).toThrow(InvalidXmlError);
  });
});

describe('metadata flattener', () => {
  it('names nested fields with dots and array positions with indexes', () => {
    const fields = flattenMetadata({
      title: 'Board minutes',
      creator: { name: 'Records Office', unit: { code: 7 } },
      keywords: ['board', 'minutes'],
      restricted: false,
      supersedes: null,
    });

    expect(Object.fromEntries(fields)).toEqual({
      title: 'Board minutes',
      'creator.name': 'Records Office',
      'creator.unit.code': '7',
      'keywords.0': 'board',
      'keywords.1': 'minutes',
      restricted: 'false',
      supersedes: 'null',
    });
  });

  it('keeps a scalar document under the empty name', () => {
    expect(Object.fromEntries(flattenMetadata('loose value'))).toEqual({ '': 'loose value' });
  });
});
