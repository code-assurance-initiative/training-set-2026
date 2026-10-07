import type { AnchorHTMLAttributes, MouseEvent } from 'react';
import { navigate } from '../routing';

type AppLinkProps = AnchorHTMLAttributes<HTMLAnchorElement> & { readonly href: string };

/** An ordinary link to an in-app path; a plain left click is handled without reloading the page. */
export function AppLink({ href, onClick, children, ...rest }: AppLinkProps) {
  const handleClick = (event: MouseEvent<HTMLAnchorElement>) => {
    onClick?.(event);
    const modified = event.metaKey || event.ctrlKey || event.shiftKey || event.altKey;
    if (event.defaultPrevented || event.button !== 0 || modified) {
      return;
    }
    event.preventDefault();
    navigate(href);
  };

  return (
    <a href={href} onClick={handleClick} {...rest}>
      {children}
    </a>
  );
}
