/** Archive document numbers: a series code, the year and a sequence, e.g. `HR-2024-001337`. */
const DOCUMENT_NUMBER = /^[A-Z]{2,4}-\d{4}-\d{1,6}$/;

export function isDocumentNumber(value: string): boolean {
  return value.length <= 16 && DOCUMENT_NUMBER.test(value);
}
