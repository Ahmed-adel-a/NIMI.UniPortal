import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile } from '../../api/client';
import { Button } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/PageHeader';
import { EmptyState, QueryState } from '../../components/ui/States';
import { addDays, formatDate, formatTime, isoDate, startOfWeek } from '../../lib/format';

export function SchedulePage() {
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const [weekStart, setWeekStart] = useState(() => startOfWeek(new Date()));
  const from = isoDate(weekStart);
  const to = isoDate(addDays(weekStart, 6));
  const days: string[] = t('days', { returnObjects: true }) as unknown as string[];

  const schedule = useQuery({
    queryKey: ['schedule', from, to],
    queryFn: () => call(api.GET('/api/schedule', { params: { query: { from, to } } })),
  });

  return (
    <>
      <PageHeader
        title={t('schedule.title')}
        subtitle={`${formatDate(weekStart, i18n.language)} — ${formatDate(addDays(weekStart, 6), i18n.language)}`}
        actions={
          <>
            <Button variant="secondary" onClick={() => setWeekStart(addDays(weekStart, -7))}>{t('schedule.prev')}</Button>
            <Button variant="secondary" onClick={() => setWeekStart(startOfWeek(new Date()))}>{t('schedule.thisWeek')}</Button>
            <Button variant="secondary" onClick={() => setWeekStart(addDays(weekStart, 7))}>{t('schedule.next')}</Button>
            <Button variant="ghost" onClick={() => void downloadFile(`/api/schedule/export?from=${from}&to=${to}`, `schedule-${from}.csv`)}>{t('schedule.exportCsv')}</Button>
            <Button variant="ghost" onClick={() => void downloadFile(`/api/documents/schedule?from=${from}&to=${to}`, `schedule-${from}.pdf`)}>{t('schedule.exportPdf')}</Button>
          </>
        }
      />
      <QueryState query={schedule}>
        {(occurrences) =>
          occurrences.length === 0 ? (
            <EmptyState>{t('schedule.empty')}</EmptyState>
          ) : (
            <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
              {Array.from({ length: 7 }, (_, i) => addDays(weekStart, i)).map((day) => {
                const items = occurrences.filter((o) => o.date === isoDate(day)).sort((a, b) => a.startTime.localeCompare(b.startTime));
                if (items.length === 0) return null;
                return (
                  <section key={isoDate(day)} className="rounded-xl border border-line bg-surface">
                    <h2 className="border-b border-line px-4 py-2.5 font-semibold">
                      {days[day.getDay()]} <span className="text-sm font-normal text-muted">{formatDate(day, i18n.language)}</span>
                    </h2>
                    <ul className="grid gap-2 p-3">
                      {items.map((o) => (
                        <li key={`${o.scheduleId}-${o.date}`} className="rounded-lg bg-sunk px-3 py-2">
                          <div className="flex items-center justify-between gap-2">
                            <span className="font-semibold">{ar ? o.subjectNameAr : o.subjectName}</span>
                            <span className="text-sm ltr-nums">{formatTime(o.startTime)}–{formatTime(o.endTime)}</span>
                          </div>
                          <p className="text-xs text-muted">
                            <span className="ltr-nums">{o.subjectCode}</span> · {t('dashboard.group')} <span className="ltr-nums">{o.groupNumber}</span> · {o.facilityName}
                          </p>
                        </li>
                      ))}
                    </ul>
                  </section>
                );
              })}
            </div>
          )
        }
      </QueryState>
    </>
  );
}
