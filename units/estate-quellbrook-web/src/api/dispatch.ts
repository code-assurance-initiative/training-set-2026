import { request } from './http';
import type { BoardRoute } from './types';

export async function getBoard(date: string, signal?: AbortSignal): Promise<readonly BoardRoute[]> {
  return (
    (await request<BoardRoute[]>(
      `/dispatch/board?date=${encodeURIComponent(date)}`,
      signal ? { signal } : {},
    )) ?? []
  );
}

export async function startRoute(routeId: string): Promise<void> {
  await request(`/dispatch/routes/${encodeURIComponent(routeId)}/start`, { method: 'POST' });
}
