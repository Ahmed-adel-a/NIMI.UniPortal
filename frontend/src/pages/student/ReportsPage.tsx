import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Badge, type Tone } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { SelectField, TextArea, TextField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { formatDateTime } from '../../lib/format';

const categories = ['academic', 'attendance', 'conduct', 'other'] as const;
const statusTone: Record<string, Tone> = { open: 'warning', answered: 'primary', closed: 'neutral' };

/** Decision ROLES2: a report goes to the head of the student's department, who answers once. */
export function ReportsPage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [category, setCategory] = useState<(typeof categories)[number]>('academic');
  const [title, setTitle] = useState('');
  const [message, setMessage] = useState('');

  const mine = useQuery({ queryKey: ['reports', 'mine'], queryFn: () => call(api.GET('/api/reports/mine', { params: { query: { pageSize: 50 } } })) });
  const send = useMutation({
    mutationFn: () => call(api.POST('/api/reports', { body: { category, title, message } })),
    onSuccess: () => {
      setTitle('');
      setMessage('');
      void queryClient.invalidateQueries({ queryKey: ['reports'] });
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    send.mutate();
  }

  return (
    <>
      <PageHeader title={t('reports.title')} subtitle={t('reports.hint')} />
      <div className="grid gap-6 lg:grid-cols-[24rem_1fr]">
        <Card title={t('reports.new')}>
          <form className="grid gap-4" onSubmit={submit}>
            <SelectField label={t('reports.category')} value={category} onChange={(e) => setCategory(e.target.value as typeof category)}>
              {categories.map((c) => (
                <option key={c} value={c}>{t(`reports.categories.${c}`)}</option>
              ))}
            </SelectField>
            <TextField label={t('reports.subject')} value={title} onChange={(e) => setTitle(e.target.value)} maxLength={200} required />
            <TextArea label={t('reports.message')} value={message} onChange={(e) => setMessage(e.target.value)} maxLength={4000} required />
            {send.isSuccess && <Alert tone="success">{t('reports.sent')}</Alert>}
            {send.isError && <Alert>{errorMessage(send.error)}</Alert>}
            <Button type="submit" busy={send.isPending}>{t('app.send')}</Button>
          </form>
        </Card>

        <Card title={t('reports.mine')}>
          <QueryState query={mine}>
            {(data) =>
              data.items.length === 0 ? (
                <EmptyState>{t('reports.empty')}</EmptyState>
              ) : (
                <ul className="grid gap-3">
                  {data.items.map((r) => (
                    <li key={r.id} className="rounded-lg border border-line p-3">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <h3 className="font-semibold">{r.title}</h3>
                        <span className="flex items-center gap-2">
                          <Badge>{t(`reports.categories.${r.category}`)}</Badge>
                          <Badge tone={statusTone[r.status] ?? 'neutral'}>{t(`reports.statuses.${r.status}`)}</Badge>
                        </span>
                      </div>
                      <p className="mt-1 whitespace-pre-line text-sm">{r.message}</p>
                      <p className="mt-1 text-xs text-muted">{formatDateTime(r.createdAt, i18n.language)}</p>
                      <div className="mt-3 rounded-lg bg-sunk p-3 text-sm">
                        <p className="text-xs font-semibold text-muted">{t('reports.reply')}{r.answeredByName ? ` — ${r.answeredByName}` : ''}</p>
                        <p className="mt-1 whitespace-pre-line">{r.reply ?? t('reports.noReply')}</p>
                      </div>
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
