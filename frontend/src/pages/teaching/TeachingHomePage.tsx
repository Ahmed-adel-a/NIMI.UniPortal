import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../auth/useAuth';
import { Card } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { EmptyState, QueryState } from '../../components/ui/States';
import { formatDate, formatTime, jsDayOf } from '../../lib/format';
import { useMyClasses, useTeachingWeek } from './useTeaching';

export function TeachingHomePage() {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const ar = i18n.language === 'ar';
  const classes = useMyClasses();
  const week = useTeachingWeek();
  const days: string[] = t('days', { returnObjects: true }) as unknown as string[];

  return (
    <>
      <PageHeader title={t('dashboard.greeting', { name: user?.fullName ?? '' })} subtitle={t('teach.title')} />
      <div className="grid gap-6 xl:grid-cols-[1fr_24rem]">
        <Card title={t('teach.classes')}>
          <QueryState query={classes}>
            {(list) =>
              list.length === 0 ? (
                <EmptyState>{t('teach.noClasses')}</EmptyState>
              ) : (
                <div className="grid gap-3 sm:grid-cols-2">
                  {list.map((c) => (
                    <Link key={c.groupId} to={`/teach/classes/${c.groupId}`} className="rounded-xl border border-line p-4 transition hover:border-primary hover:bg-primary-soft">
                      <p className="font-semibold">{ar ? c.subjectNameAr : c.subjectName}</p>
                      <p className="mt-1 text-sm text-muted">
                        <span className="ltr-nums">{c.subjectCode}</span> · {t('teach.group', { n: c.groupNumber })}
                      </p>
                    </Link>
                  ))}
                </div>
              )
            }
          </QueryState>
        </Card>
        <Card title={t('teach.week')}>
          <QueryState query={week}>
            {(items) =>
              items.length === 0 ? (
                <EmptyState>{t('teach.noLectures')}</EmptyState>
              ) : (
                <ul className="grid gap-2">
                  {items.map((o) => (
                    <li key={`${o.scheduleId}-${o.date}`} className="rounded-lg bg-sunk px-3 py-2">
                      <div className="flex items-center justify-between gap-2">
                        <span className="text-sm font-semibold">{ar ? o.subjectNameAr : o.subjectName}</span>
                        <span className="text-xs ltr-nums">{formatTime(o.startTime)}–{formatTime(o.endTime)}</span>
                      </div>
                      <p className="text-xs text-muted">
                        {days[jsDayOf(o.dayOfWeek)]} {formatDate(o.date, i18n.language)} · <span className="ltr-nums">{o.subjectCode}</span> ·{' '}
                        {t('teach.group', { n: o.groupNumber })} · {o.facilityName}
                      </p>
                    </li>
                  ))}
                </ul>
              )
            }
          </QueryState>
        </Card>
      </div>
    </>
  );
}
