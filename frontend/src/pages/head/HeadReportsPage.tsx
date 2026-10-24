import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import type { Schema } from '../../api/types';
import { Badge, type Tone } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pager } from '../../components/ui/Pager';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { formatDateTime } from '../../lib/format';

const statuses = ['open', 'answered', 'closed'] as const;
const statusTone: Record<string, Tone> = { open: 'warning', answered: 'primary', closed: 'neutral' };

/** Decision ROLES2: the department head's inbox of their students' reports. */
export function HeadReportsPage() {
  const { t } = useTranslation();
  const [status, setStatus] = useState<(typeof statuses)[number] | null>('open');
  const [page, setPage] = useState(1);

  const inbox = useQuery({
    queryKey: ['department-reports', status, page],
    queryFn: () => call(api.GET('/api/department/reports', { params: { query: { status: status ?? undefined, page, pageSize: 20 } } })),
  });

  return (
    <>
      <PageHeader title={t('head.title')} />
      <div className="mb-4 flex flex-wrap gap-2">
        {[null, ...statuses].map((s) => (
          <Button key={s ?? 'all'} variant={status === s ? 'primary' : 'secondary'} className="min-h-8" onClick={() => { setStatus(s); setPage(1); }}>
            {s ? t(`reports.statuses.${s}`) : t('head.all')}
          </Button>
        ))}
      </div>
      <QueryState query={inbox}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('head.empty')}</EmptyState>
          ) : (
            <div className="grid gap-3">
              {data.items.map((r) => <ReportItem key={r.id} report={r} />)}
              <Pager page={page} totalPages={data.totalPages ?? 1} onPage={setPage} />
            </div>
          )
        }
      </QueryState>
    </>
  );
}

function ReportItem({ report: r }: { report: Schema<'StudentReportDto'> }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [reply, setReply] = useState('');
  const [closeToo, setCloseToo] = useState(false);

  const answer = useMutation({
    mutationFn: (v: { reply: string | null; close: boolean }) =>
      call(api.POST('/api/department/reports/{id}/answer', { params: { path: { id: r.id } }, body: v })),
    onSuccess: () => {
      setReply('');
      void queryClient.invalidateQueries({ queryKey: ['department-reports'] });
    },
  });

  return (
    <article className="rounded-xl border border-line bg-surface p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="font-semibold">{r.title}</h2>
        <span className="flex items-center gap-2">
          <Badge>{t(`reports.categories.${r.category}`)}</Badge>
          <Badge tone={statusTone[r.status] ?? 'neutral'}>{t(`reports.statuses.${r.status}`)}</Badge>
        </span>
      </div>
      <p className="mt-1 text-xs text-muted">
        {t('head.from', { name: r.studentName, code: r.studentCode ?? '' })} · {formatDateTime(r.createdAt, i18n.language)}
      </p>
      <p className="mt-2 whitespace-pre-line text-sm">{r.message}</p>
      {r.reply && (
        <div className="mt-3 rounded-lg bg-sunk p-3 text-sm">
          <p className="text-xs font-semibold text-muted">{t('head.reply')}{r.answeredByName ? ` — ${r.answeredByName}` : ''}</p>
          <p className="mt-1 whitespace-pre-line">{r.reply}</p>
        </div>
      )}
      {r.status !== 'closed' && (
        <div className="mt-3 grid gap-2">
          {r.status === 'open' && (
            <>
              <textarea
                aria-label={t('head.reply')}
                placeholder={t('head.reply')}
                rows={3}
                className="w-full rounded-lg border border-line bg-surface px-3 py-2 text-sm"
                value={reply}
                onChange={(e) => setReply(e.target.value)}
              />
              <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={closeToo} onChange={(e) => setCloseToo(e.target.checked)} />{t('head.closeToo')}</label>
            </>
          )}
          <div className="flex flex-wrap gap-2">
            {r.status === 'open' && (
              <Button busy={answer.isPending} disabled={!reply.trim()} onClick={() => answer.mutate({ reply, close: closeToo })}>{t('head.answer')}</Button>
            )}
            <Button variant="secondary" busy={answer.isPending} onClick={() => answer.mutate({ reply: null, close: true })}>{t('head.close')}</Button>
          </div>
          {answer.isError && <Alert>{errorMessage(answer.error)}</Alert>}
        </div>
      )}
    </article>
  );
}
