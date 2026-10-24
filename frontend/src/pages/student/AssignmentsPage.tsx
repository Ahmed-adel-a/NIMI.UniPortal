import { useState } from 'react';
import { useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, sendForm } from '../../api/client';
import type { Schema } from '../../api/types';
import { Badge, type Tone } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { Table } from '../../components/ui/Table';
import { formatDateTime, formatNumber } from '../../lib/format';
import { useMyCourses } from './useMyCourses';

type Assignment = Schema<'AssignmentDto'>;
const statusTone: Record<string, Tone> = { submitted: 'primary', approved: 'success', rejected: 'danger' };

export function AssignmentsPage() {
  const { t, i18n } = useTranslation();
  const courses = useMyCourses();
  const submissions = useQuery({ queryKey: ['submissions', 'mine'], queryFn: () => call(api.GET('/api/submissions/mine', { params: { query: { pageSize: 100 } } })) });

  // One list per course: the API lists assignments per subject.
  const perCourse = useQueries({
    queries: (courses.data ?? []).map((c) => ({
      queryKey: ['assignments', c.subjectId],
      queryFn: () => call(api.GET('/api/assignments', { params: { query: { subjectId: c.subjectId, pageSize: 100 } } })),
    })),
  });
  const assignments = perCourse.flatMap((q) => q.data?.items ?? []).sort((a, b) => a.dueAt.localeCompare(b.dueAt));
  const loading = courses.isPending || perCourse.some((q) => q.isPending);
  const open = assignments.filter((a) => a.isOpen);
  const closed = assignments.filter((a) => !a.isOpen);

  return (
    <>
      <PageHeader title={t('assignments.title')} />
      <div className="grid gap-6">
        <Card title={`${t('assignments.open')} (${open.length})`}>
          {loading ? <p className="text-muted">{t('app.loading')}</p> : open.length === 0 ? <EmptyState>{t('assignments.empty')}</EmptyState> : (
            <ul className="grid gap-3">{open.map((a) => <AssignmentItem key={a.id} assignment={a} />)}</ul>
          )}
        </Card>

        <Card title={t('assignments.mine')}>
          <QueryState query={submissions}>
            {(data) =>
              data.items.length === 0 ? (
                <EmptyState>{t('assignments.noSubmissions')}</EmptyState>
              ) : (
                <Table head={[t('assignments.title'), t('fees.receipt'), t('fees.date'), '', t('assignments.grade'), t('assignments.feedback')]}>
                  {data.items.map((s) => (
                    <tr key={s.id}>
                      <td>
                        <span className="font-medium">{s.assignmentTitle}</span> <span className="text-xs text-muted ltr-nums">{s.subjectCode}</span>
                      </td>
                      <td className="ltr-nums">{s.receiptNumber}</td>
                      <td>{formatDateTime(s.submittedAt, i18n.language)}</td>
                      <td><Badge tone={statusTone[s.status] ?? 'neutral'}>{t(`assignments.statuses.${s.status}`)}</Badge></td>
                      <td className="ltr-nums">{s.grade === null ? '—' : `${formatNumber(s.grade, i18n.language)} / ${formatNumber(s.maxGrade, i18n.language)}`}</td>
                      <td className="text-sm">{s.feedback ?? ''}</td>
                    </tr>
                  ))}
                </Table>
              )
            }
          </QueryState>
        </Card>

        {closed.length > 0 && (
          <Card title={`${t('assignments.closed')} (${closed.length})`}>
            <ul className="grid gap-3">{closed.map((a) => <AssignmentItem key={a.id} assignment={a} />)}</ul>
          </Card>
        )}
      </div>
    </>
  );
}

function AssignmentItem({ assignment: a }: { assignment: Assignment }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [file, setFile] = useState<File | null>(null);

  const submit = useMutation({
    mutationFn: () => {
      const form = new FormData();
      form.append('file', file!);
      return sendForm<Schema<'SubmissionReceiptDto'>>(`/api/assignments/${a.id}/submissions`, form);
    },
    onSuccess: () => {
      setFile(null);
      void queryClient.invalidateQueries({ queryKey: ['assignments'] });
      void queryClient.invalidateQueries({ queryKey: ['submissions'] });
    },
  });

  return (
    <li className="rounded-lg border border-line p-3">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <p className="font-semibold">{a.title}</p>
          <p className="text-xs text-muted">
            <span className="ltr-nums">{a.subjectCode}</span> · {t('assignments.due', { date: formatDateTime(a.dueAt, i18n.language) })} · {t('assignments.maxGrade')}{' '}
            <span className="ltr-nums">{formatNumber(a.maxGrade, i18n.language)}</span>
          </p>
          {a.instructions && <p className="mt-1 whitespace-pre-line text-sm">{a.instructions}</p>}
        </div>
        {a.mySubmissionStatus && <Badge tone={statusTone[a.mySubmissionStatus] ?? 'neutral'}>{t(`assignments.statuses.${a.mySubmissionStatus}`)}</Badge>}
      </div>
      {a.isOpen && !a.mySubmissionStatus && (
        <div className="mt-3 grid gap-2">
          <div className="flex flex-wrap items-center gap-2">
            <input
              type="file"
              aria-label={t('assignments.choose')}
              accept={a.allowedExtensions.map((e) => (e.startsWith('.') ? e : `.${e}`)).join(',')}
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
              className="text-sm file:me-3 file:rounded-lg file:border-0 file:bg-sunk file:px-3 file:py-2 file:text-sm"
            />
            <Button disabled={!file} busy={submit.isPending} onClick={() => submit.mutate()}>
              {t('assignments.submit')}
            </Button>
          </div>
          <p className="text-xs text-muted">{t('assignments.allowed', { ext: a.allowedExtensions.join(', '), mb: a.maxFileMb })}</p>
          {submit.isSuccess && <Alert tone="success">{t('assignments.submitted', { receipt: submit.data.receiptNumber })}</Alert>}
          {submit.isError && <Alert>{errorMessage(submit.error)}</Alert>}
        </div>
      )}
    </li>
  );
}
