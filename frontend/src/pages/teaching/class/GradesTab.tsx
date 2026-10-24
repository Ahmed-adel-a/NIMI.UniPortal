import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile, sendForm } from '../../../api/client';
import type { Schema } from '../../../api/types';
import { Badge } from '../../../components/ui/Badge';
import { Button } from '../../../components/ui/Button';
import { Alert, QueryState } from '../../../components/ui/States';
import { Table } from '../../../components/ui/Table';
import { useErrorMessage } from '../../../components/ui/useErrorMessage';
import { formatNumber } from '../../../lib/format';
import type { TeachingClass } from '../ClassPage';

/** Decision D3: teaching staff draft scores; Student Affairs confirms and publishes. Confirmed rows are read-only. */
export function GradesTab({ current }: { current: TeachingClass }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [scores, setScores] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const key = ['grades', 'group', current.groupId];

  const grades = useQuery({
    queryKey: key,
    queryFn: () => call(api.GET('/api/grades/groups/{groupId}', { params: { path: { groupId: current.groupId } } })),
  });
  const done = (data: Schema<'GroupGradesDto'>, text: string) => {
    queryClient.setQueryData(key, data);
    setScores({});
    setMessage({ tone: 'success', text });
  };

  const save = useMutation({
    mutationFn: () =>
      call(api.PUT('/api/grades/groups/{groupId}', {
        params: { path: { groupId: current.groupId } },
        body: { entries: Object.entries(scores).filter(([, v]) => v.trim() !== '').map(([studentId, v]) => ({ studentId, score: Number(v) })) },
      })),
    onSuccess: (data) => done(data, t('teach.grades.saved')),
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });
  const importCsv = useMutation({
    mutationFn: (file: File) => {
      const form = new FormData();
      form.append('file', file);
      return sendForm<Schema<'GroupGradesDto'>>(`/api/grades/groups/${current.groupId}/import`, form);
    },
    onSuccess: (data) => done(data, t('teach.grades.imported')),
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  return (
    <div className="grid gap-3">
      <div className="flex flex-wrap items-center gap-2">
        <Button busy={save.isPending} disabled={Object.keys(scores).length === 0} onClick={() => save.mutate()}>{t('teach.grades.save')}</Button>
        <Button variant="secondary" onClick={() => void downloadFile(`/api/grades/groups/${current.groupId}/template`, `grades-${current.subjectCode}-G${current.groupNumber}.csv`)}>
          {t('teach.grades.template')}
        </Button>
        <label className="inline-flex min-h-10 cursor-pointer items-center rounded-lg border border-line bg-surface px-4 text-sm font-semibold hover:bg-sunk">
          {t('teach.grades.import')}
          <input type="file" accept=".csv,text/csv" className="sr-only" onChange={(e) => { const f = e.target.files?.[0]; if (f) importCsv.mutate(f); e.target.value = ''; }} />
        </label>
      </div>
      {message && <Alert tone={message.tone}>{message.text}</Alert>}
      <QueryState query={grades}>
        {(data) => (
          <Table head={[t('teach.roster.student'), t('teach.roster.code'), t('teach.grades.score'), t('grades.letter'), t('teach.grades.status')]}>
            {data.rows.map((row) => {
              const locked = row.status === 'confirmed' || row.status === 'published';
              return (
                <tr key={row.studentId}>
                  <td className="font-medium">{row.fullName}</td>
                  <td className="ltr-nums">{row.studentCode}</td>
                  <td>
                    <input
                      type="number"
                      inputMode="decimal"
                      min={0}
                      max={100}
                      step="0.5"
                      dir="ltr"
                      aria-label={`${t('teach.grades.score')} ${row.fullName}`}
                      disabled={locked}
                      className="w-24 rounded-lg border border-line bg-surface px-2 py-1 disabled:opacity-60"
                      value={scores[row.studentId] ?? (row.score ?? '').toString()}
                      onChange={(e) => setScores({ ...scores, [row.studentId]: e.target.value })}
                    />
                  </td>
                  <td className="font-semibold ltr-nums">{row.grade ?? '—'}{row.gradePoints !== null ? ` (${formatNumber(row.gradePoints, i18n.language)})` : ''}</td>
                  <td>{row.status ? <Badge tone={row.status === 'published' ? 'success' : row.status === 'confirmed' ? 'primary' : 'neutral'}>{t(`teach.grades.statuses.${row.status}`)}</Badge> : '—'}</td>
                </tr>
              );
            })}
          </Table>
        )}
      </QueryState>
    </div>
  );
}
