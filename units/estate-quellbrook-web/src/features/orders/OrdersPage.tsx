import { useState } from 'react';
import { listOrders } from '../../api/orders';
import { ErrorMessage } from '../../components/ErrorMessage';
import { useLoad } from '../../useLoad';
import { matching } from './orderFilter';
import { OrdersTable } from './OrdersTable';

export function OrdersPage() {
  const [page, setPage] = useState(1);
  const [orders] = useLoad((signal) => listOrders(page, signal), String(page));
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('all');

  return (
    <section aria-labelledby="orders-heading">
      <h1 id="orders-heading">Orders</h1>
      <div className="filters">
        <input
          type="search"
          placeholder="Search consignee or city"
          value={search}
          onChange={(event) => {
            setSearch(event.target.value);
          }}
        />
        <label htmlFor="status-filter">Status</label>
        <select
          id="status-filter"
          value={status}
          onChange={(event) => {
            setStatus(event.target.value);
          }}
        >
          <option value="all">All</option>
          <option value="placed">Placed</option>
          <option value="cancelled">Cancelled</option>
        </select>
      </div>
      {orders.state === 'loading' && <p role="status">Loading orders…</p>}
      {orders.state === 'failed' && <ErrorMessage error={orders.error} />}
      {orders.state === 'loaded' && (
        <>
          <OrdersTable orders={matching(orders.value.items, search, status)} />
          <nav className="paging" aria-label="Pages">
            <button
              type="button"
              disabled={page === 1}
              onClick={() => {
                setPage(page - 1);
              }}
            >
              Newer
            </button>
            <span>
              Page {page} of{' '}
              {Math.max(1, Math.ceil(orders.value.totalCount / orders.value.pageSize))}
            </span>
            <button
              type="button"
              disabled={page * orders.value.pageSize >= orders.value.totalCount}
              onClick={() => {
                setPage(page + 1);
              }}
            >
              Older
            </button>
          </nav>
        </>
      )}
    </section>
  );
}
