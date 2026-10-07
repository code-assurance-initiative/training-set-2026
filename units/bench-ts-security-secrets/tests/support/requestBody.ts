/** The body a stubbed fetch received, as text. */
export function bodyText(init: RequestInit | undefined): string {
  const body = init?.body;
  if (typeof body === "string") {
    return body;
  }
  if (body instanceof URLSearchParams) {
    return body.toString();
  }
  throw new Error("expected a string or URLSearchParams request body");
}
