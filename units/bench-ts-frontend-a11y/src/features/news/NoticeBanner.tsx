import DOMPurify from 'dompurify';
import { useEffect, useMemo, useState } from 'react';
import type { LibraryNotice } from '../../api/types';
import { useCatalogueClient } from '../../context/catalogueClient';

const NOTICE_MARKUP = {
  ALLOWED_TAGS: ['p', 'strong', 'em', 'a', 'ul', 'li', 'br'],
  ALLOWED_ATTR: ['href'],
};

/** The current staff notice (closures, changed hours), shown above every page. */
export function NoticeBanner() {
  const client = useCatalogueClient();
  const [notice, setNotice] = useState<LibraryNotice | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    client.currentNotice(controller.signal).then(setNotice, () => {
      setNotice(null);
    });
    return () => {
      controller.abort();
    };
  }, [client]);

  const safeHtml = useMemo(
    () => (notice ? DOMPurify.sanitize(notice.bodyHtml, NOTICE_MARKUP) : ''),
    [notice],
  );

  if (!notice) {
    return null;
  }

  return (
    <aside className="notice" aria-label="Library notice">
      <p className="notice__title">{notice.title}</p>
      <div className="notice__body" dangerouslySetInnerHTML={{ __html: safeHtml }} />
    </aside>
  );
}
