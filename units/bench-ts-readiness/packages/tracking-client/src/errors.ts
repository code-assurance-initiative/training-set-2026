/** An RFC 9457 problem answer from the API. */
export class TrackingApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail?: string,
  ) {
    super(detail === undefined ? `${status} ${title}` : `${status} ${title}: ${detail}`);
    this.name = "TrackingApiError";
  }
}
