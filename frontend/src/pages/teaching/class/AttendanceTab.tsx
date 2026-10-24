import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../../api/client';
import { QrImage } from '../../../components/QrImage';
import { Badge, type Tone } from '../../../components/ui/Badge';
import { Button } from '../../../components/ui/Button';
import { Card } from '../../../components/ui/Card';
import { Alert, EmptyState, QueryState } from '../../../components/ui/States';
import { useErrorMessage } from '../../../components/ui/useErrorMessage';
import { formatDate, formatTime, jsDayOf } from '../../../lib/format';
import type { TeachingClass } from '../ClassPage';
import { useTeachingWeek } from '../useTeaching';

const statusTone: Record<string, Tone> = { present: 'success', late: 'warning', manual_audit: 'primary', absent: 'danger', not_yet: 'neutral' };

/** The session id per lecture, kept for this tab so a reload can resume (the API has no "find my session" read). */
function sessionKey(slotId: string, date: string) {
  return `nimi.attendance.${slotId}.${date}`;
}

function rememberedSession(slotId: string, date: string): string | null {
  try {
    return sessionStorage.getItem(sessionKey(slotId, date));
  } catch {
    return null;
  }
}

export function AttendanceTab({ current }: { current: TeachingClass }) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const week = useTeachingWeek();
  const days: string[] = t('days', { returnObjects: true }) as unknown as string[];
  const [active, setActive] = useState<{ id: string; date: string } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const open = useMutation({
    mutationFn: (o: { slotId: string; date: string }) => call(api.POST('/api/attendance/sessions', { body: { slotId: o.slotId, date: o.date } })),
    onSuccess: (session, o) => {
      try {
        sessionStorage.setItem(sessionKey(o.slotId, o.date), session.id);
      } catch {
        // without storage the session can't be resumed after a reload
      }
      setActive({ id: session.id, date: o.date });
      setError(null);
    },
    onError: (e) => setError(errorMessage(e)),
  });

  if (active) return <LiveSession sessionId={active.id} date={active.date} onDone={() => setActive(null)} />;

  return (
    <Card title={t('teach.attendance.occurrences')}>
      {error && <div className="mb-3"><Alert>{error}</Alert></div>}
      <QueryState query={week}>
        {(items) => {
          const mine = items.filter((o) => o.subjectCode === current.subjectCode && o.groupNumber === current.groupNumber);
          if (mine.length === 0) return <EmptyState>{t('teach.attendance.none')}</EmptyState>;
          return (
            <ul className="grid gap-2">
              {mine.map((o) => {
                const remembered = rememberedSession(o.scheduleId, o.date);
                return (
                  <li key={`${o.scheduleId}-${o.date}`} className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-sunk px-3 py-2">
                    <span className="text-sm">
                      {days[jsDayOf(o.dayOfWeek)]} {formatDate(o.date, i18n.language)} · <span className="ltr-nums">{formatTime(o.startTime)}–{formatTime(o.endTime)}</span> · {o.facilityName}
                    </span>
                    {remembered ? (
                      <Button variant="secondary" onClick={() => setActive({ id: remembered, date: o.date })}>{t('teach.attendance.resume')}</Button>
                    ) : (
                      <Button busy={open.isPending && open.variables?.slotId === o.scheduleId && open.variables?.date === o.date} onClick={() => open.mutate({ slotId: o.scheduleId, date: o.date })}>
                        {t('teach.attendance.openSession')}
                      </Button>
                    )}
                  </li>
                );
              })}
            </ul>
          );
        }}
      </QueryState>
    </Card>
  );
}

/** The open session: the rotating code to show the class, the live roster, manual entries and closing. */
function LiveSession({ sessionId, date, onDone }: { sessionId: string; date: string; onDone: () => void }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [note, setNote] = useState('');
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);

  const live = useQuery({
    queryKey: ['attendance', 'live', sessionId],
    queryFn: () => call(api.GET('/api/attendance/sessions/{id}/live', { params: { path: { id: sessionId } } })),
    refetchInterval: 5_000,
  });
  const isOpen = live.data?.isOpen ?? true;
  const qr = useQuery({
    queryKey: ['attendance', 'qr', sessionId],
    queryFn: () => call(api.GET('/api/attendance/sessions/{id}/qr', { params: { path: { id: sessionId } } })),
    refetchInterval: 15_000,
    enabled: isOpen,
  });

  const manual = useMutation({
    mutationFn: (v: { studentId: string; status: 'present' | 'late' | 'absent' }) =>
      call(api.POST('/api/attendance/sessions/{id}/manual', { params: { path: { id: sessionId } }, body: { studentId: v.studentId, status: v.status, note } })),
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['attendance', 'live', sessionId] }); setMessage(null); },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });
  const close = useMutation({
    mutationFn: () => call(api.POST('/api/attendance/sessions/{id}/close', { params: { path: { id: sessionId } } })),
    onSuccess: (s) => {
      setMessage({ tone: 'success', text: t('teach.attendance.closed', { present: s.present, late: s.late, manual: s.manualAudit, absent: s.absent }) });
      void queryClient.invalidateQueries({ queryKey: ['attendance'] });
    },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  return (
    <div className="grid gap-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="text-lg font-semibold">{t('teach.attendance.session', { date: formatDate(date, i18n.language) })}</h2>
        <div className="flex gap-2">
          {isOpen && <Button variant="danger" busy={close.isPending} onClick={() => close.mutate()}>{t('teach.attendance.close')}</Button>}
          <Button variant="secondary" onClick={onDone}>{t('app.back')}</Button>
        </div>
      </div>
      {message && <Alert tone={message.tone}>{message.text}</Alert>}

      {isOpen && (
        <Card title={t('teach.attendance.code')}>
          <QueryState query={qr}>
            {(code) => (
              <div className="grid justify-items-center gap-3 py-2">
                <QrImage value={code.token} label={t('teach.attendance.code')} />
                <p className="text-sm text-muted">{t('teach.attendance.codeHint')}</p>
                <Button variant="ghost" className="min-h-8" onClick={() => void navigator.clipboard?.writeText(code.token).catch(() => undefined)}>
                  {t('teach.attendance.copy')}
                </Button>
              </div>
            )}
          </QueryState>
        </Card>
      )}

      <Card title={t('teach.attendance.live')}>
        <QueryState query={live}>
          {(roster) => (
            <div className="grid gap-3">
              <p className="text-sm text-muted">
                {t('teach.attendance.counts', { present: roster.counts.present, late: roster.counts.late, manual: roster.counts.manualAudit, absent: roster.counts.absent, notYet: roster.counts.notYet })}
              </p>
              {roster.isOpen && (
                <input
                  className="w-full rounded-lg border border-line bg-surface px-3 py-2 text-sm"
                  placeholder={t('teach.attendance.note')}
                  aria-label={t('teach.attendance.note')}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              )}
              {roster.isOpen && !note.trim() && <p className="text-xs text-muted">{t('teach.attendance.noteFirst')}</p>}
              <ul className="grid gap-1">
                {roster.items.map((s) => (
                  <li key={s.studentId} className="flex flex-wrap items-center justify-between gap-2 rounded-lg px-2 py-1.5 hover:bg-sunk">
                    <span className="text-sm">
                      {s.fullName} <span className="text-xs text-muted ltr-nums">{s.studentCode}</span>
                    </span>
                    <span className="flex items-center gap-1">
                      <Badge tone={statusTone[s.status] ?? 'neutral'}>{t(`teach.attendance.statuses.${s.status}`, { defaultValue: s.status })}</Badge>
                      {roster.isOpen &&
                        (['present', 'late', 'absent'] as const).map((status) => (
                          <Button
                            key={status}
                            variant="ghost"
                            className="min-h-7 px-2 text-xs"
                            disabled={!note.trim()}
                            onClick={() => manual.mutate({ studentId: s.studentId, status })}
                          >
                            {t(`teach.attendance.statuses.${status}`)}
                          </Button>
                        ))}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </QueryState>
      </Card>
    </div>
  );
}
