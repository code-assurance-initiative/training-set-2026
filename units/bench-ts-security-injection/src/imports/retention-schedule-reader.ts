import libxmljs from 'libxmljs2';
import { childElements, InvalidXmlError, wellFormed } from './xml-elements.js';

export interface RetentionRule {
  readonly series: string;
  readonly years: number;
}

/** Reads a `<retention-schedule>` document: `<rule series="HR" years="10"/>` per series. */
export function readRetentionSchedule(xml: string): RetentionRule[] {
  const document = wellFormed(() => libxmljs.parseXml(xml));
  const root = document.root();
  if (root?.name() !== 'retention-schedule') {
    throw new InvalidXmlError('Expected a <retention-schedule> document.');
  }
  return childElements(root)
    .filter((element) => element.name() === 'rule')
    .map((rule) => {
      const series = rule.attr('series')?.value() ?? '';
      const years = Number(rule.attr('years')?.value());
      if (!/^[A-Z]{2,4}$/.test(series) || !Number.isInteger(years) || years < 1 || years > 200) {
        throw new InvalidXmlError(`Invalid retention rule on line ${rule.line()}.`);
      }
      return { series, years };
    });
}
