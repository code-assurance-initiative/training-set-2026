import { ApiError } from '../api/http';

export function ErrorMessage({ error }: { error: unknown }) {
  const text =
    error instanceof ApiError
      ? error.status === 401
        ? 'Your session has ended. Reload the page to sign in again.'
        : error.detail || error.title
      : 'Something went wrong. Try again in a moment.';
  return (
    <p className="error" role="alert">
      {text}
    </p>
  );
}
