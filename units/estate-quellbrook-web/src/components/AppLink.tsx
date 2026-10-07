import type { MouseEvent, ReactNode } from 'react';
import { navigate } from '../routing';

/** An ordinary link that navigates inside the console without a page load. */
export function AppLink({
  to,
  children,
  current,
}: {
  to: string;
  children: ReactNode;
  current?: boolean;
}) {
  const onClick = (event: MouseEvent<HTMLAnchorElement>) => {
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey) {
      return;
    }
    event.preventDefault();
    navigate(to);
  };
  return (
    <a href={to} onClick={onClick} aria-current={current ? 'page' : undefined}>
      {children}
    </a>
  );
}
