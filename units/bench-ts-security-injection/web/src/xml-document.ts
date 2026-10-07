/** Parses an XML metadata file chosen by the user; throws when it is not well-formed. */
export function parseMetadataXml(xml: string): Document {
  const document = new DOMParser().parseFromString(xml, 'application/xml');
  if (document.getElementsByTagName('parsererror').length > 0) {
    throw new Error('The file is not well-formed XML.');
  }
  return document;
}

/** The first element of that name in the metadata, if any. */
export function firstField(document: Document, field: string): Element | null {
  return document.getElementsByTagName(field).item(0);
}
