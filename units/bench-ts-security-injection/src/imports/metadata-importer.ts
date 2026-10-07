import libxmljs from 'libxmljs2';
import { childElements, InvalidXmlError, wellFormed } from './xml-elements.js';

export interface MetadataField {
  readonly name: string;
  readonly value: string;
}

/**
 * Reads the `<metadata>` document the scanning stations upload with each batch: one child element
 * per field. Stations declare recurring values (agency, series titles) as entities in the internal
 * subset, so entities are substituted while parsing.
 */
export function readMetadata(xml: string): MetadataField[] {
  const document = wellFormed(() => libxmljs.parseXml(xml, { noent: true, noblanks: true }));
  const root = document.root();
  if (root?.name() !== 'metadata') {
    throw new InvalidXmlError('Expected a <metadata> document.');
  }
  return childElements(root).map((field) => ({ name: field.name(), value: field.text().trim() }));
}
