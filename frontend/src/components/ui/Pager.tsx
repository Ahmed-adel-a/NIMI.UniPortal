import { useTranslation } from 'react-i18next';
import { Button } from './Button';

export function Pager({ page, totalPages, onPage }: { page: number; totalPages: number; onPage: (page: number) => void }) {
  const { t } = useTranslation();
  if (totalPages <= 1) return null;
  return (
    <div className="flex items-center justify-center gap-3 pt-2 text-sm">
      <Button variant="secondary" disabled={page <= 1} onClick={() => onPage(page - 1)}>
        {t('app.back')}
      </Button>
      <span className="ltr-nums">
        {page} / {totalPages}
      </span>
      <Button variant="secondary" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>
        →
      </Button>
    </div>
  );
}
