import { Layout } from './components/Layout';
import { DispatchBoard } from './features/dispatch/DispatchBoard';
import { OrderDetailPage } from './features/orders/OrderDetailPage';
import { OrdersPage } from './features/orders/OrdersPage';
import { useRoute } from './routing';

export function App() {
  const route = useRoute();
  return (
    <Layout route={route}>
      {route.name === 'orders' && <OrdersPage />}
      {route.name === 'order' && <OrderDetailPage orderId={route.orderId} />}
      {route.name === 'dispatch' && <DispatchBoard />}
      {route.name === 'not-found' && (
        <section>
          <h1>Page not found</h1>
        </section>
      )}
    </Layout>
  );
}
