import type { Element, Node } from 'libxmljs2';

export class InvalidXmlError extends Error {
  constructor(message: string, options?: { cause: unknown }) {
    super(message, options);
    this.name = 'InvalidXmlError';
  }
}

/** Runs a libxml parse, turning its parse errors into InvalidXmlError. */
export function wellFormed<T>(parse: () => T): T {
  try {
    return parse();
  } catch (error) {
    throw new InvalidXmlError('The document is not well-formed XML.', { cause: error });
  }
}

/** The element children of `node`, skipping text, comments and processing instructions. */
export function childElements(node: Element): Element[] {
  return node.childNodes().filter((child: Node): child is Element => child.type() === 'element');
}
