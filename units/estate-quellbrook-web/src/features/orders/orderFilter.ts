import type { OrderSummary } from '../../api/types';

/** Narrows the loaded page by consignee or city and by status. */
export function matching(
  orders: readonly OrderSummary[],
  search: string,
  status: string,
): readonly OrderSummary[] {
  const needle = search.trim().toLocaleLowerCase();
  return orders.filter(
    (order) =>
      (status === 'all' || order.status.toLowerCase() === status) &&
      (needle === '' ||
        order.consigneeName.toLocaleLowerCase().includes(needle) ||
        order.destinationCity.toLocaleLowerCase().includes(needle)),
  );
}
