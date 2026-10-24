import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { ApiError } from '../../api/errors';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { SelectField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { Table } from '../../components/ui/Table';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { localName } from '../../i18n';
import { formatNumber } from '../../lib/format';
import { useCurrentTerm, useDepartments } from './lookups';

const statusTone = { draft: 'warning', confirmed: 'primary', published: 'success' } as const;

/** Decision D3: teaching staff draft, Student Affairs confirms complete groups and publishes them. */
export function GradeApprovalPage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const term = useCurrentTerm();
  const departments = useDepartments();
  const termId = term.data?.id;
  const [departmentId, setDepartmentId] = useState('');
  const [subjectId, setSubjectId] = useState('');
  const [groupId, setGroupId] = useState('');
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);

  const subjects = useQuery({
    queryKey: ['admin', 'subjects', departmentId, ''],
    queryFn: () => call(api.GET('/api/subjects', { params: { query: { departmentId: departmentId || undefined, pageSize: 100 } } })),
  });
  const groups = useQuery({
    queryKey: ['admin', 'groups', subjectId, termId],
    queryFn: () => call(api.GET('/api/subjects/{subjectId}/groups', { params: { path: { subjectId }, query: { termId, pageSize: 100 } } })),
    enabled: !!subjectId && !!termId,
  });
  const sheet = useQuery({
    queryKey: ['admin', 'grade-sheet', groupId],
    queryFn: () => call(api.GET('/api/grades/groups/{groupId}', { params: { path: { groupId } } })),
    enabled: !!groupId,
  });

  const done = (text: string) => {
    void queryClient.invalidateQueries({ queryKey: ['admin', 'grade-sheet'] });
    setMessage({ tone: 'success', text });
  };
  const onError = (e: unknown) => setMessage({ tone: 'danger', text: errorMessage(e) });

  const confirm = useMutation({
    mutationFn: () => call(api.POST('/api/admin/grades/confirm', { body: { groupId } })),
    onSuccess: (r) => done(t('admin.grades.confirmed', { n: r.confirmed })),
    onError: (e) => {
      // Rule R3: the 400 names every roster student without a grade; the sheet below already shows who.
      const missing = e instanceof ApiError ? (e.body?.details ?? []).filter((d) => d.field === 'missingStudentIds').length : 0;
      if (missing > 0) setMessage({ tone: 'danger', text: t('admin.grades.incomplete', { n: missing }) });
      else onError(e);
    },
  });
  const publish = useMutation({
    mutationFn: (scope: 'group' | 'term') =>
      call(api.POST('/api/admin/grades/publish', { body: scope === 'group' ? { groupId, termId: null, subjectId: null } : { termId: termId!, subjectId: null, groupId: null } })),
    onSuccess: (r) => done(t('admin.grades.published', { n: r.published })),
    onError,
  });

  if (!term.isPending && !termId) {
    return (
      <>
        <PageHeader title={t('admin.grades.title')} />
        <EmptyState>{t('admin.common.noActiveTerm')}</EmptyState>
      </>
    );
  }

  return (
    <>
      <PageHeader
        title={t('admin.grades.title')}
        subtitle={t('admin.grades.hint')}
        actions={<Button variant="secondary" busy={publish.isPending && publish.variables === 'term'} onClick={() => window.confirm(t('admin.grades.confirmPublishTerm')) && publish.mutate('term')}>{t('admin.grades.publishTerm')}</Button>}
      />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      <div className="mb-4 grid gap-3 sm:grid-cols-3">
        <SelectField label={t('admin.common.department')} value={departmentId} onChange={(e) => { setDepartmentId(e.target.value); setSubjectId(''); setGroupId(''); }}>
          <option value="">{t('admin.common.all')}</option>
          {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
        </SelectField>
        <SelectField label={t('admin.subjects.title')} value={subjectId} onChange={(e) => { setSubjectId(e.target.value); setGroupId(''); }}>
          <option value="">{t('admin.common.select')}</option>
          {(subjects.data?.items ?? []).map((s) => <option key={s.id} value={s.id}>{s.code} — {localName(s, i18n.language)}</option>)}
        </SelectField>
        <SelectField label={t('admin.grades.group')} value={groupId} onChange={(e) => setGroupId(e.target.value)} disabled={!subjectId}>
          <option value="">{t('admin.common.select')}</option>
          {(groups.data?.items ?? []).map((g) => <option key={g.id} value={g.id}>{g.groupNumber}</option>)}
        </SelectField>
      </div>
      {groupId && (
        <QueryState query={sheet}>
          {(data) => {
            const counts = { missing: 0, draft: 0, confirmed: 0, published: 0 };
            for (const row of data.rows) counts[row.status ?? 'missing']++;
            return (
              <Card
                title={<><span className="ltr-nums">{data.subjectCode}</span> — {localName({ name: data.subjectName, nameAr: data.subjectNameAr }, i18n.language)} · {t('admin.grades.group')} {data.groupNumber}</>}
                actions={
                  <>
                    <Button busy={confirm.isPending} disabled={counts.draft === 0} onClick={() => confirm.mutate()}>{t('admin.grades.confirm')}</Button>
                    <Button variant="secondary" busy={publish.isPending && publish.variables === 'group'} disabled={counts.confirmed === 0} onClick={() => publish.mutate('group')}>{t('admin.grades.publishGroup')}</Button>
                  </>
                }
              >
                <p className="mb-3 text-sm text-muted">
                  {t('admin.grades.counts', counts)}
                </p>
                {data.rows.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
                  <Table head={[t('profile.studentCode'), t('profile.fullName'), t('teach.grades.score'), t('grades.letter'), t('teach.grades.status')]}>
                    {data.rows.map((r) => (
                      <tr key={r.studentId}>
                        <td className="ltr-nums">{r.studentCode ?? '—'}</td>
                        <td>{r.fullName}</td>
                        <td className="ltr-nums">{formatNumber(r.score, i18n.language)}</td>
                        <td className="ltr-nums">{r.grade ?? '—'}{r.gradePoints !== null && r.gradePoints !== undefined ? ` (${r.gradePoints})` : ''}</td>
                        <td>{r.status ? <Badge tone={statusTone[r.status]}>{t(`teach.grades.statuses.${r.status}`)}</Badge> : <Badge tone="danger">{t('admin.grades.missing')}</Badge>}</td>
                      </tr>
                    ))}
                  </Table>
                )}
              </Card>
            );
          }}
        </QueryState>
      )}
    </>
  );
}
