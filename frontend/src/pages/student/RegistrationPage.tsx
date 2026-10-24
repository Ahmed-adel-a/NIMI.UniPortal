import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Badge, type Tone } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, Stat } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { formatNumber, formatTime, jsDayOf } from '../../lib/format';

const statusTone: Record<string, Tone> = { registered: 'success', pending_payment: 'warning', completed: 'primary', failed: 'danger', dropped: 'neutral' };

/** Credit hours: register in the window, see seats and the reasons a course isn't open to you, drop. */
export function RegistrationPage() {
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const days: string[] = t('days', { returnObjects: true }) as unknown as string[];
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);

  const offerings = useQuery({ queryKey: ['offerings'], queryFn: () => call(api.GET('/api/registration/offerings', { params: { query: { pageSize: 100 } } })) });
  const mine = useQuery({ queryKey: ['registrations'], queryFn: () => call(api.GET('/api/registration/mine')) });
  const progress = useQuery({ queryKey: ['progress'], queryFn: () => call(api.GET('/api/registration/progress')) });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['offerings'] });
    void queryClient.invalidateQueries({ queryKey: ['registrations'] });
    void queryClient.invalidateQueries({ queryKey: ['schedule'] });
  };
  const register = useMutation({
    mutationFn: (subjectGroupId: string) => call(api.POST('/api/registration', { body: { subjectGroupId } })),
    onSuccess: () => { setMessage({ tone: 'success', text: t('registration.registered') }); refresh(); },
    onError: (error) => setMessage({ tone: 'danger', text: errorMessage(error) }),
  });
  const drop = useMutation({
    mutationFn: (registrationId: string) => call(api.DELETE('/api/registration/{registrationId}', { params: { path: { registrationId } } })),
    onSuccess: () => { setMessage({ tone: 'success', text: t('registration.dropped') }); refresh(); },
    onError: (error) => setMessage({ tone: 'danger', text: errorMessage(error) }),
  });

  return (
    <>
      <PageHeader title={t('registration.title')} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}

      <QueryState query={progress}>
        {(p) => (
          <div className="mb-6 grid grid-cols-1 gap-3 sm:grid-cols-3">
            <Stat label={t('registration.earned')} value={<span className="ltr-nums">{p.creditsEarned}</span>} />
            <Stat label={t('registration.inProgress')} value={<span className="ltr-nums">{p.creditsInProgress}</span>} />
            <Stat label={t('registration.gpa')} value={<span className="ltr-nums">{formatNumber(p.cumulativeGpa, i18n.language)}</span>} />
          </div>
        )}
      </QueryState>

      <div className="grid gap-6 xl:grid-cols-[1fr_22rem]">
        <Card title={t('registration.offerings')}>
          <QueryState query={offerings}>
            {(data) =>
              data.items.length === 0 ? (
                <EmptyState>{t('registration.empty')}</EmptyState>
              ) : (
                <ul className="grid gap-3">
                  {data.items.map((o) => (
                    <li key={o.groupId} className="rounded-lg border border-line p-3">
                      <div className="flex flex-wrap items-start justify-between gap-2">
                        <div className="min-w-0">
                          <p className="font-semibold">{ar ? o.subjectNameAr : o.subjectName}</p>
                          <p className="text-xs text-muted">
                            <span className="ltr-nums">{o.subjectCode}</span> · {t('dashboard.group')} <span className="ltr-nums">{o.groupNumber}</span> ·{' '}
                            <span className="ltr-nums">{o.creditHours}</span> {t('registration.credits')} · {t('registration.seats')}: <span className="ltr-nums">{o.seatsLeft}</span>
                          </p>
                          <p className="mt-1 text-xs text-muted">
                            {o.slots.map((s) => `${days[jsDayOf(s.dayOfWeek)]} ${formatTime(s.startTime)}–${formatTime(s.endTime)}`).join(' · ')}
                          </p>
                          {!o.eligible && o.reasons.length > 0 && <p className="mt-1 text-xs text-warning">{o.reasons.join(' ')}</p>}
                        </div>
                        <Button disabled={!o.eligible} busy={register.isPending && register.variables === o.groupId} onClick={() => register.mutate(o.groupId)}>
                          {o.eligible ? t('registration.register') : t('registration.notEligible')}
                        </Button>
                      </div>
                    </li>
                  ))}
                </ul>
              )
            }
          </QueryState>
        </Card>

        <Card title={t('registration.mine')}>
          <QueryState query={mine}>
            {(m) => (
              <div className="grid gap-3">
                <p className="text-sm">
                  {t('registration.total')}: <strong className="ltr-nums">{m.totalCredits}</strong>
                  {m.minCredits !== null && m.maxCredits !== null && (
                    <span className="block text-xs text-muted">{t('registration.limits', { min: m.minCredits, max: m.maxCredits })}</span>
                  )}
                </p>
                {m.belowMinimum && <Alert tone="warning">{t('registration.belowMin')}</Alert>}
                {m.registrations.length === 0 ? (
                  <EmptyState>{t('registration.noneRegistered')}</EmptyState>
                ) : (
                  <ul className="grid gap-2">
                    {m.registrations.map((r) => (
                      <li key={r.id} className="flex items-center justify-between gap-2 rounded-lg bg-sunk px-3 py-2">
                        <div className="min-w-0">
                          <p className="text-sm font-semibold">{ar ? r.subjectNameAr : r.subjectName}</p>
                          <p className="text-xs text-muted">
                            <span className="ltr-nums">{r.subjectCode}</span> · <Badge tone={statusTone[r.status] ?? 'neutral'}>{t(`registration.status.${r.status}`)}</Badge>
                          </p>
                        </div>
                        {r.status === 'registered' && (
                          <Button
                            variant="ghost"
                            className="min-h-8 px-2 text-danger"
                            busy={drop.isPending && drop.variables === r.id}
                            onClick={() => { if (window.confirm(t('registration.confirmDrop'))) drop.mutate(r.id); }}
                          >
                            {t('registration.drop')}
                          </Button>
                        )}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            )}
          </QueryState>
        </Card>
      </div>
    </>
  );
}
