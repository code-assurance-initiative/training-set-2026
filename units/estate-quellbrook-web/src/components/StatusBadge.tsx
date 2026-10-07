const labels: Record<string, string> = {
  placed: 'Placed',
  cancelled: 'Cancelled',
  AwaitingRoute: 'Waiting for a route',
  Assigned: 'On a route',
  OutForDelivery: 'Out for delivery',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled',
};

/** A status in words; the colour only repeats what the text says. */
export function StatusBadge({ status }: { status: string }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{labels[status] ?? status}</span>;
}
