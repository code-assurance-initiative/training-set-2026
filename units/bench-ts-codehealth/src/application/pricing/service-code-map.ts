import type { ServiceLevel } from './quote.js';

/** The carrier's product code for a service level, as printed on the label and sent to its API. */
export function serviceCode(
  carrier: string,
  serviceLevel: ServiceLevel,
  international: boolean,
): string {
  switch (`${carrier}:${serviceLevel}:${international ? 'intl' : 'dom'}`) {
    case 'alder:economy:dom':
      return 'ALD-ECO';
    case 'alder:economy:intl':
      return 'ALD-ECO-X';
    case 'alder:standard:dom':
      return 'ALD-STD';
    case 'alder:standard:intl':
      return 'ALD-STD-X';
    case 'alder:express:dom':
      return 'ALD-EXP';
    case 'alder:express:intl':
      return 'ALD-EXP-X';
    case 'corvid:economy:dom':
      return 'CV10';
    case 'corvid:economy:intl':
      return 'CV10I';
    case 'corvid:standard:dom':
      return 'CV20';
    case 'corvid:standard:intl':
      return 'CV20I';
    case 'corvid:express:dom':
      return 'CV30';
    case 'corvid:express:intl':
      return 'CV30I';
    case 'corvid:express-saturday:dom':
      return 'CV31';
    case 'corvid:express-saturday:intl':
      return 'CV31I';
    case 'alder:pallet:dom':
      return 'ALD-PAL';
    case 'corvid:pallet:dom':
      return 'CV90';
    default:
      throw new RangeError(`No ${serviceLevel} service from ${carrier}`);
  }
}
