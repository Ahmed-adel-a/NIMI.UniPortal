import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { ApiError } from '../../api/errors';
import { Badge } from '../../components/ui/Badge';
import { Card, Stat } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, EmptyState, ErrorState } from '../../components/ui/States';
import { Spinner } from '../../components/ui/Spinner';

/** Year groups (نظام الفرق): the student's year, status and carried subjects. */
export function StandingPage() {
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const standing = useQuery({ queryKey: ['standing'], queryFn: () => call(api.GET('/api/standing')), retry: false });

  return (
    <>
      <PageHeader title={t('standing.title')} />
      {standing.isPending ? (
        <Spinner />
      ) : standing.isError ? (
        standing.error instanceof ApiError && standing.error.status === 404 ? <Alert tone="info">{t('standing.none')}</Alert> : <ErrorState error={standing.error} onRetry={() => standing.refetch()} />
      ) : (
        <div className="grid gap-6">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Stat label={t('standing.level')} value={<span>{standing.data.level} <span className="text-base font-normal text-muted">{t('standing.of', { n: standing.data.finalLevel })}</span></span>} />
            <Stat label={t('standing.years')} value={<span className="ltr-nums">{standing.data.yearsInLevel}</span>} />
            <Stat label={t('standing.status')} value={<Badge tone={standing.data.status === 'active' ? 'success' : 'warning'}>{t(`standing.statuses.${standing.data.status}`)}</Badge>} />
          </div>
          <Card title={t('standing.carried')}>
            {standing.data.carriedSubjects.length === 0 ? (
              <EmptyState>{t('standing.noCarried')}</EmptyState>
            ) : (
              <ul className="grid gap-2">
                {standing.data.carriedSubjects.map((s) => (
                  <li key={s.subjectId} className="rounded-lg bg-sunk px-3 py-2">
                    <span className="ltr-nums text-muted">{s.code}</span> · {ar ? s.nameAr : s.name}
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
      )}
    </>
  );
}
