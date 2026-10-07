import type { ReactNode } from 'react';
import type { Route } from '../routing';
import { AppLink } from './AppLink';

export function Layout({ route, children }: { route: Route; children: ReactNode }) {
  return (
    <>
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <header className="site-header">
        <div className="brand">
          <img src="/icons/logo.svg" alt="Quellbrook Freight" width="32" height="32" />
          <span className="brand-name">Operations</span>
        </div>
        <nav aria-label="Main">
          <ul>
            <li>
              <AppLink to="/orders" current={route.name === 'orders' || route.name === 'order'}>
                Orders
              </AppLink>
            </li>
            <li>
              <AppLink to="/dispatch" current={route.name === 'dispatch'}>
                Dispatch board
              </AppLink>
            </li>
          </ul>
        </nav>
      </header>
      <img className="divider" src="/icons/divider.svg" alt="" aria-hidden="true" />
      <main id="main" tabIndex={-1}>
        {children}
      </main>
    </>
  );
}
