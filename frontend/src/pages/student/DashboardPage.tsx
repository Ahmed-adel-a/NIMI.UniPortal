import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { useAuth } from '../../auth/useAuth';
import { Badge } from '../../components/ui/Badge';
import { Card, Stat } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { formatDate, formatDateTime, formatMoney, formatNumber, formatTime } from '../../lib/format';

export function DashboardPage() {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const ar = i18n.language === 'ar';
  const dashboard = useQuery({ queryKey: ['dashboard'], queryFn: () => call(api.GET('/api/dashboard')) });

  return (
    <>
      <PageHeader title={t('dashboard.greeting', { name: user?.fullName ?? '' })} subtitle={formatDate(new Date(), i18n.language)} />
      <QueryState query={dashboard}>
        {(d) => (
          <div className="grid gap-6">
            {d.unavailable.length > 0 && <Alert tone="warning">{t('errors.unavailable')}</Alert>}
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
              <Stat label={t('dashboard.attendanceRate')} value={d.attendanceRate === null ? '—' : `${formatNumber(d.attendanceRate, i18n.language, 0)}%`} />
              <Stat label={t('dashboard.unread')} value={<span className="ltr-nums">{d.unreadNotifications ?? '—'}</span>} />
              <Stat
                label={t('dashboard.outstanding')}
                value={d.outstandingFees === null ? '—' : `${formatMoney(d.outstandingFees, i18n.language)} ${t('fees.currency')}`}
                tone={Number(d.outstandingFees ?? 0) > 0 ? 'warning' : 'success'}
              />
            </div>

            <div className="grid gap-6 lg:grid-cols-2">
              <Card title={t('dashboard.today')} actions={<Link to="/student/schedule" className="text-sm text-primary hover:underline">{t('nav.schedule')}</Link>}>
                {(d.today ?? []).length === 0 ? (
                  <EmptyState>{t('dashboard.noLectures')}</EmptyState>
                ) : (
                  <ul className="grid gap-2">
                    {(d.today ?? []).map((l, index) => (
                      <li key={`${l.subjectId}-${index}`} className="flex items-center justify-between gap-3 rounded-lg bg-sunk px-3 py-2">
                        <div className="min-w-0">
                          <p className="font-semibold">{ar ? l.subjectNameAr : l.subjectName}</p>
                          <p className="text-xs text-muted">
                            <span className="ltr-nums">{l.subjectCode}</span> · {t('dashboard.group')} <span className="ltr-nums">{l.groupNumber}</span> · {l.facilityName}
                          </p>
                        </div>
                        <span className="shrink-0 text-sm font-semibold ltr-nums">
                          {formatTime(l.startTime)}–{formatTime(l.endTime)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </Card>

              <Card title={t('dashboard.upcoming')} actions={<Link to="/student/assignments" className="text-sm text-primary hover:underline">{t('nav.assignments')}</Link>}>
                {(d.upcomingAssignments ?? []).length === 0 ? (
                  <EmptyState>{t('dashboard.noAssignments')}</EmptyState>
                ) : (
                  <ul className="grid gap-2">
                    {(d.upcomingAssignments ?? []).map((a) => (
                      <li key={a.id} className="flex items-center justify-between gap-3 rounded-lg bg-sunk px-3 py-2">
                        <div className="min-w-0">
                          <p className="font-semibold">{a.title}</p>
                          <p className="text-xs text-muted ltr-nums">{a.subjectCode}</p>
                        </div>
                        <span className="shrink-0 text-xs text-warning">{formatDateTime(a.dueAt, i18n.language)}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </Card>

              <Card title={t('dashboard.announcements')} className="lg:col-span-2" actions={<Link to="/student/courses" className="text-sm text-primary hover:underline">{t('nav.courses')}</Link>}>
                {(d.recentAnnouncements ?? []).length === 0 ? (
                  <EmptyState>{t('dashboard.noAnnouncements')}</EmptyState>
                ) : (
                  <ul className="grid gap-2">
                    {(d.recentAnnouncements ?? []).map((a) => (
                      <li key={a.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-sunk px-3 py-2">
                        <span className="font-medium">
                          <span className="text-muted ltr-nums">{a.subjectCode}</span> · {a.title}
                        </span>
                        <span className="flex items-center gap-2">
                          {a.priority !== 'normal' && <Badge tone={a.priority === 'urgent' ? 'danger' : 'warning'}>{t(`courses.priority.${a.priority}`)}</Badge>}
                          <time className="text-xs text-muted">{formatDate(a.createdAt, i18n.language)}</time>
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </Card>
            </div>
          </div>
        )}
      </QueryState>
    </>
  );
}
