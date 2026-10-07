import type { OrderSummary } from '../../api/types';
import { StatusBadge } from '../../components/StatusBadge';
import { navigate } from '../../routing';

const placed = new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short' });

export function OrdersTable({ orders }: { orders: readonly OrderSummary[] }) {
  return (
    <table className="orders">
      <caption className="visually-hidden">Orders, newest first</caption>
      <thead>
        <tr>
          <th scope="col">Placed</th>
          <th scope="col">Consignee</th>
          <th scope="col">Destination</th>
          <th scope="col">Service</th>
          <th scope="col">Parcels</th>
          <th scope="col">Status</th>
        </tr>
      </thead>
      <tbody>
        {orders.map((order) => (
          <tr
            key={order.orderId}
            className="clickable"
            onClick={() => {
              navigate(`/orders/${order.orderId}`);
            }}
          >
            <td>{placed.format(new Date(order.placedAt))}</td>
            <td>{order.consigneeName}</td>
            <td>{order.destinationCity}</td>
            <td>{order.serviceLevel}</td>
            <td>{order.parcelCount}</td>
            <td>
              <StatusBadge status={order.status.toLowerCase()} />
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
