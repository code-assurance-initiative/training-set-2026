import { renderQueue, type QueuedFile } from './document-list.js';
import { renderPreview } from './document-preview.js';
import { recentRecipients, rememberRecipients } from './recent-recipients.js';
import { loadViewSettings, saveViewSettings, type ViewSettings } from './view-settings.js';

function element<T extends HTMLElement>(id: string): T {
  const found = document.getElementById(id);
  if (!found) {
    throw new Error(`The page has no element #${id}.`);
  }
  return found as T;
}

const queue: QueuedFile[] = [];

function showError(message: string): void {
  element('upload-error').textContent = message;
}

element<HTMLInputElement>('metadata-file').addEventListener('change', (event) => {
  const input = event.currentTarget as HTMLInputElement;
  const file = input.files?.item(0);
  if (!file) {
    return;
  }
  file.text().then(
    (xml) => {
      renderPreview(element('preview'), xml);
      queue.push({ fileName: file.name, xml });
      renderQueue(element('queue'), queue);
      showError('');
    },
    () => {
      showError('The file could not be read.');
    },
  );
});

const recipients = element<HTMLDataListElement>('recent-recipients');
for (const address of recentRecipients()) {
  const option = document.createElement('option');
  option.value = address;
  recipients.append(option);
}

element<HTMLFormElement>('delivery-form').addEventListener('submit', (event) => {
  const form = event.currentTarget as HTMLFormElement;
  const addresses = new FormData(form)
    .getAll('recipient')
    .filter((value): value is string => typeof value === 'string' && value.length > 0);
  rememberRecipients(addresses);
});

const settings = loadViewSettings();
document.documentElement.dataset.theme = settings.theme;
element<HTMLSelectElement>('page-size').value = String(settings.pageSize);
element<HTMLSelectElement>('page-size').addEventListener('change', (event) => {
  const pageSize = Number(
    (event.currentTarget as HTMLSelectElement).value,
  ) as ViewSettings['pageSize'];
  saveViewSettings({ ...loadViewSettings(), pageSize });
});
