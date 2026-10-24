import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from './Button';
import { Spinner } from './Spinner';
import { useErrorMessage } from './useErrorMessage';

export function EmptyState({ children }: { children: ReactNode }) {
  return <p className="rounded-lg border border-dashed border-line px-4 py-8 text-center text-sm text-muted">{children}</p>;
}

export function Alert({ tone = 'danger', children }: { tone?: 'danger' | 'success' | 'warning' | 'info'; children: ReactNode }) {
  const style = {
    danger: 'border-danger/30 bg-danger-soft text-danger',
    success: 'border-success/30 bg-success-soft text-success',
    warning: 'border-warning/30 bg-warning-soft text-warning',
    info: 'border-primary/30 bg-primary-soft text-primary',
  }[tone];
  return (
    <div role={tone === 'danger' ? 'alert' : 'status'} className={`rounded-lg border px-4 py-3 text-sm ${style}`}>
      {children}
    </div>
  );
}

export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const { t } = useTranslation();
  const message = useErrorMessage();
  return (
    <div className="grid justify-items-start gap-3">
      <Alert>{message(error)}</Alert>
      {onRetry && (
        <Button variant="secondary" onClick={onRetry}>
          {t('app.retry')}
        </Button>
      )}
    </div>
  );
}

/** Loading → spinner, failure → error with retry, otherwise the content. */
export function QueryState<T>({ query, children }: { query: { isPending: boolean; isError: boolean; error: unknown; data: T | undefined; refetch: () => unknown }; children: (data: T) => ReactNode }) {
  const { t } = useTranslation();
  if (query.isPending)
    return (
      <div className="flex items-center gap-2 py-6 text-muted">
        <Spinner />
        {t('app.loading')}
      </div>
    );
  if (query.isError) return <ErrorState error={query.error} onRetry={() => query.refetch()} />;
  return <>{children(query.data as T)}</>;
}
