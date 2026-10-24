import { useTranslation } from 'react-i18next';

export function Spinner({ className = '' }: { className?: string }) {
  return (
    <span
      role="status"
      aria-live="polite"
      className={`inline-block size-5 animate-spin rounded-full border-2 border-line border-t-primary ${className}`}
    />
  );
}

export function FullPageSpinner() {
  const { t } = useTranslation();
  return (
    <div className="flex min-h-dvh items-center justify-center gap-3 text-muted">
      <Spinner />
      <span>{t('app.loading')}</span>
    </div>
  );
}
