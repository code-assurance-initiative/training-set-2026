import { useState } from 'react';
import { getShipment } from '../../api/orders';
import type { Delivery } from '../../api/types';
import { AppLink } from '../../components/AppLink';
import { ErrorMessage } from '../../components/ErrorMessage';
import { StatusBadge } from '../../components/StatusBadge';
import { useLoad } from '../../useLoad';
import { CancelOrderDialog } from './CancelOrderDialog';

const time = new Intl.DateTimeFormat('en-GB', { dateStyle: 'medium', timeStyle: 'short' });

export function OrderDetailPage({ orderId }: { orderId: string }) {
  const [shipment, reload] = useLoad((signal) => getShipment(orderId, signal), orderId);
  const [cancelling, setCancelling] = useState(false);

  if (shipment.state === 'loading') {
    return <p role="status">Loading the order…</p>;
  }
  if (shipment.state === 'failed' || !shipment.value) {
    return shipment.state === 'failed' ? <ErrorMessage error={shipment.error} /> : null;
  }
  const { order, delivery } = shipment.value;
  const cancellable =
    order.status === 'placed' &&
    (delivery === null || delivery.status === 'AwaitingRoute' || delivery.status === 'Assigned');
  return (
    <article aria-labelledby="order-heading">
      <p>
        <AppLink to="/orders">Back to orders</AppLink>
      </p>
      <h1 id="order-heading">Order for {order.consignee.name}</h1>
      <dl className="facts">
        <dt>Status</dt>
        <dd>
          <StatusBadge status={order.status} />
        </dd>
        <dt>Customer account</dt>
        <dd>{order.customerAccountId}</dd>
        <dt>Service</dt>
        <dd>{order.serviceLevel}</dd>
        <dt>Deliver to</dt>
        <dd>
          {order.consignee.line1}
          {order.consignee.line2 ? `, ${order.consignee.line2}` : ''}, {order.consignee.postalCode}{' '}
          {order.consignee.city}, {order.consignee.countryCode}
        </dd>
        <dt>Parcels</dt>
        <dd>
          {order.parcels.length} parcels, {(order.totalWeightGrams / 1000).toFixed(1)} kg
        </dd>
        <dt>Placed by</dt>
        <dd>{order.placedBy}</dd>
      </dl>
      <h2>Delivery</h2>
      <DeliveryStatus delivery={delivery} />
      {cancellable && (
        <button
          type="button"
          onClick={() => {
            setCancelling(true);
          }}
        >
          Cancel order…
        </button>
      )}
      {cancelling && (
        <CancelOrderDialog
          orderId={order.id}
          onClose={() => {
            setCancelling(false);
          }}
          onCancelled={() => {
            setCancelling(false);
            reload();
          }}
        />
      )}
    </article>
  );
}

function DeliveryStatus({ delivery }: { delivery: Delivery | null }) {
  if (delivery === null) {
    return <p>Not with dispatch yet.</p>;
  }
  return (
    <p>
      <StatusBadge status={delivery.status} />
      {delivery.outForDeliveryAt && <> since {time.format(new Date(delivery.outForDeliveryAt))}</>}
      {delivery.deliveredAt && <> on {time.format(new Date(delivery.deliveredAt))}</>}
      {delivery.proof && <> ({delivery.proof})</>}
    </p>
  );
}
