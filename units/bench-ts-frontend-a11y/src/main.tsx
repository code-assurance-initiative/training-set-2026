import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createHttpClient } from './api/catalogueClient';
import { createDemoClient } from './api/demoClient';
import { App } from './App';
import { CatalogueClientContext } from './context/catalogueClient';
import './styles/base.css';
import './styles/header.css';
import './styles/buttons.css';
import './styles/search.css';
import './styles/catalogue.css';
import './styles/loading.css';
import './styles/dialog.css';
import './styles/hero.css';
import './styles/gallery.css';
import './styles/loans.css';
import './styles/reading-list.css';
import './styles/settings.css';
import './styles/toast.css';

const rootElement = document.getElementById('root');
if (!rootElement) {
  throw new Error('The page has no #root element to render into');
}

const client = import.meta.env.DEV ? createDemoClient() : createHttpClient('/api');

createRoot(rootElement).render(
  <StrictMode>
    <CatalogueClientContext value={client}>
      <App />
    </CatalogueClientContext>
  </StrictMode>,
);
