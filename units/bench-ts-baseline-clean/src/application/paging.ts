export interface PageRequest {
  readonly offset: number;
  readonly limit: number;
}

export interface Page<T> {
  readonly items: readonly T[];
  readonly total: number;
  readonly offset: number;
  readonly limit: number;
}

export function paginate<T>(items: readonly T[], request: PageRequest): Page<T> {
  return {
    items: items.slice(request.offset, request.offset + request.limit),
    total: items.length,
    offset: request.offset,
    limit: request.limit,
  };
}
