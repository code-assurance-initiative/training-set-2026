import { NAVIGATION, hrefFor, type PageName } from '../routing';
import { AppLink } from './AppLink';

interface SiteHeaderProps {
  readonly current: PageName;
}

export function SiteHeader({ current }: SiteHeaderProps) {
  return (
    <header className="site-header">
      <a className="skip-link" href="#main-content">
        Skip to main content
      </a>
      <AppLink className="site-header__brand" href={hrefFor('home')}>
        <img src="/images/logo.svg" alt="Community Library home" width="160" height="40" />
      </AppLink>
      <nav aria-label="Main">
        <ul className="site-nav">
          {NAVIGATION.map((entry) => (
            <li key={entry.page}>
              <AppLink
                className="site-nav__link"
                href={hrefFor(entry.page)}
                aria-current={entry.page === current ? 'page' : undefined}
              >
                {entry.label}
              </AppLink>
            </li>
          ))}
        </ul>
      </nav>
    </header>
  );
}
