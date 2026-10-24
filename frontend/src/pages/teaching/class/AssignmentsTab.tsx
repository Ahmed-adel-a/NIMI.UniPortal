import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile } from '../../../api/client';
import type { Schema } from '../../../api/types';
import { Badge, type Tone } from '../../../components/ui/Badge';
import { Button } from '../../../components/ui/Button';
import { Card } from '../../../components/ui/Card';
import { TextArea, TextField } from '../../../components/ui/Field';
import { Alert, EmptyState, QueryState } from '../../../components/ui/States';
import { useErrorMessage } from '../../../components/ui/useErrorMessage';
import { formatDateTime, formatNumber } from '../../../lib/format';
import type { TeachingClass } from '../ClassPage';

const statusTone: Record<string, Tone> = { submitted: 'primary', approved: 'success', rejected: 'danger' };

export function AssignmentsTab({ current }: { current: TeachingClass }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [form, setForm] = useState({ title: '', instructions: '', dueAt: '', maxGrade: '10', extensions: 'pdf,zip', maxMb: '10', thisGroup: false });
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const key = ['assignments', current.subjectId];

  const list = useQuery({
    queryKey: key,
    queryFn: () => call(api.GET('/api/assignments', { params: { query: { subjectId: current.subjectId, pageSize: 100 } } })),
  });
  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/assignments', {
        body: {
          subjectId: current.subjectId,
          subjectGroupId: form.thisGroup ? current.groupId : null,
          title: form.title,
          instructions: form.instructions || null,
          dueAt: new Date(form.dueAt).toISOString(),
          maxGrade: Number(form.maxGrade),
          allowedExtensions: form.extensions || null,
          maxFileMb: form.maxMb ? Number(form.maxMb) : null,
        },
      })),
    onSuccess: () => {
      setForm({ ...form, title: '', instructions: '', dueAt: '' });
      setMessage({ tone: 'success', text: t('teach.assignments.created') });
      void queryClient.invalidateQueries({ queryKey: key });
    },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[22rem_1fr]">
      <Card title={t('teach.assignments.new')}>
        <form className="grid gap-3" onSubmit={submit}>
          <TextField label={t('teach.assignments.title')} value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required />
          <TextArea label={t('teach.assignments.instructions')} value={form.instructions} onChange={(e) => setForm({ ...form, instructions: e.target.value })} />
          <TextField label={t('teach.assignments.due')} type="datetime-local" value={form.dueAt} onChange={(e) => setForm({ ...form, dueAt: e.target.value })} required dir="ltr" />
          <TextField label={t('teach.assignments.maxGrade')} type="number" min={1} value={form.maxGrade} onChange={(e) => setForm({ ...form, maxGrade: e.target.value })} required dir="ltr" />
          <TextField label={t('teach.assignments.extensions')} value={form.extensions} onChange={(e) => setForm({ ...form, extensions: e.target.value })} dir="ltr" />
          <TextField label={t('teach.assignments.maxMb')} type="number" min={1} value={form.maxMb} onChange={(e) => setForm({ ...form, maxMb: e.target.value })} dir="ltr" />
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={form.thisGroup} onChange={(e) => setForm({ ...form, thisGroup: e.target.checked })} />{t('teach.announcements.thisGroup')}</label>
          {message && <Alert tone={message.tone}>{message.text}</Alert>}
          <Button type="submit" busy={create.isPending}>{t('teach.assignments.new')}</Button>
        </form>
      </Card>
      <QueryState query={list}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('assignments.empty')}</EmptyState>
          ) : (
            <ul className="grid content-start gap-3">
              {data.items.map((a) => <AssignmentReview key={a.id} assignment={a} />)}
            </ul>
          )
        }
      </QueryState>
    </div>
  );
}

function AssignmentReview({ assignment: a }: { assignment: Schema<'AssignmentDto'> }) {
  const { t, i18n } = useTranslation();
  const [open, setOpen] = useState(false);
  const submissions = useQuery({
    queryKey: ['submissions', a.id],
    queryFn: () => call(api.GET('/api/assignments/{id}/submissions', { params: { path: { id: a.id }, query: { pageSize: 100 } } })),
    enabled: open,
  });

  return (
    <li className="rounded-lg border border-line p-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="font-semibold">{a.title}</p>
          <p className="text-xs text-muted">
            {t('assignments.due', { date: formatDateTime(a.dueAt, i18n.language) })} · {t('teach.assignments.maxGrade')} <span className="ltr-nums">{formatNumber(a.maxGrade, i18n.language)}</span>
          </p>
        </div>
        <span className="flex items-center gap-2">
          <Badge tone={a.isOpen ? 'success' : 'neutral'}>{a.isOpen ? t('assignments.open') : t('assignments.closed')}</Badge>
          <Button variant="secondary" className="min-h-8" onClick={() => setOpen(!open)}>
            {t('teach.assignments.submissions', { n: a.submissionCount ?? 0 })}
          </Button>
        </span>
      </div>
      {open && (
        <div className="mt-3">
          <QueryState query={submissions}>
            {(data) =>
              data.items.length === 0 ? (
                <EmptyState>{t('teach.assignments.noSubmissions')}</EmptyState>
              ) : (
                <ul className="grid gap-2">
                  {data.items.map((s) => <SubmissionRow key={s.id} submission={s} assignment={a} />)}
                </ul>
              )
            }
          </QueryState>
        </div>
      )}
    </li>
  );
}

function SubmissionRow({ submission: s, assignment: a }: { submission: Schema<'SubmissionDto'>; assignment: Schema<'AssignmentDto'> }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [grade, setGrade] = useState('');
  const [text, setText] = useState('');

  const review = useMutation({
    mutationFn: (decision: 'approve' | 'reject') =>
      call(api.PUT('/api/submissions/{id}/review', {
        params: { path: { id: s.id } },
        body: decision === 'approve'
          ? { decision, grade: Number(grade), feedback: text || null, rejectionReason: null }
          : { decision, grade: null, feedback: null, rejectionReason: text },
      })),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['submissions', a.id] }),
  });

  return (
    <li className="rounded-lg bg-sunk p-3 text-sm">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span>
          <span className="font-medium">{s.studentName}</span> <span className="text-xs text-muted ltr-nums">{s.studentCode} · {s.receiptNumber}</span>
        </span>
        <span className="flex items-center gap-2">
          <Badge tone={statusTone[s.status] ?? 'neutral'}>{t(`assignments.statuses.${s.status}`)}</Badge>
          <Button variant="ghost" className="min-h-8 px-2" onClick={() => void downloadFile(`/api/submissions/${s.id}/file`, s.fileName)}>{t('app.download')}</Button>
        </span>
      </div>
      <p className="text-xs text-muted">{formatDateTime(s.submittedAt, i18n.language)}</p>
      {s.status === 'submitted' ? (
        <div className="mt-2 flex flex-wrap items-end gap-2">
          <input aria-label={t('teach.assignments.grade')} placeholder={t('teach.assignments.grade')} type="number" min={0} max={Number(a.maxGrade)} step="0.5" dir="ltr" value={grade} onChange={(e) => setGrade(e.target.value)} className="w-24 rounded-lg border border-line bg-surface px-2 py-1" />
          <input aria-label={t('teach.assignments.feedback')} placeholder={`${t('teach.assignments.feedback')} / ${t('teach.assignments.reason')}`} value={text} onChange={(e) => setText(e.target.value)} className="min-w-40 flex-1 rounded-lg border border-line bg-surface px-2 py-1" />
          <Button className="min-h-8" disabled={!grade} busy={review.isPending && review.variables === 'approve'} onClick={() => review.mutate('approve')}>{t('teach.assignments.approve')}</Button>
          <Button variant="danger" className="min-h-8" disabled={!text.trim()} busy={review.isPending && review.variables === 'reject'} onClick={() => review.mutate('reject')}>{t('teach.assignments.reject')}</Button>
        </div>
      ) : (
        <p className="mt-1">
          {s.grade !== null && <span className="font-semibold ltr-nums">{formatNumber(s.grade, i18n.language)} / {formatNumber(a.maxGrade, i18n.language)}</span>} {s.feedback}
        </p>
      )}
      {review.isError && <div className="mt-2"><Alert>{errorMessage(review.error)}</Alert></div>}
    </li>
  );
}
