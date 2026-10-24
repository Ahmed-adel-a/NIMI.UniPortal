import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { SelectField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pager } from '../../components/ui/Pager';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { Table } from '../../components/ui/Table';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { localName } from '../../i18n';
import { formatDateTime } from '../../lib/format';
import { useDepartments } from './lookups';

const reportTone = { open: 'warning', answered: 'primary', closed: 'neutral' } as const;
const deviceTone = { pending: 'warning', approved: 'success', rejected: 'neutral' } as const;

/** Every department's student reports, read-only: the department head answers them. */
export function AdminReportsPage() {
  const { t, i18n } = useTranslation();
  const departments = useDepartments();
  const [departmentId, setDepartmentId] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const reports = useQuery({
    queryKey: ['admin', 'reports', departmentId, status, page],
    queryFn: () => call(api.GET('/api/admin/reports', { params: { query: { departmentId: departmentId || undefined, status: (status || undefined) as 'open' | undefined, page, pageSize: 25 } } })),
  });

  return (
    <>
      <PageHeader title={t('admin.reports.title')} subtitle={t('admin.reports.hint')} />
      <div className="mb-4 grid gap-3 sm:grid-cols-2">
        <SelectField label={t('admin.common.department')} value={departmentId} onChange={(e) => { setDepartmentId(e.target.value); setPage(1); }}>
          <option value="">{t('admin.common.all')}</option>
          {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
        </SelectField>
        <SelectField label={t('admin.common.status')} value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
          <option value="">{t('admin.common.all')}</option>
          {(['open', 'answered', 'closed'] as const).map((s) => <option key={s} value={s}>{t(`reports.statuses.${s}`)}</option>)}
        </SelectField>
      </div>
      <QueryState query={reports}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('reports.empty')}</EmptyState> : (
            <>
              <div className="grid gap-3">
                {data.items.map((r) => (
                  <details key={r.id} className="rounded-xl border border-line bg-surface p-4">
                    <summary className="flex cursor-pointer flex-wrap items-center gap-2 text-sm">
                      <Badge tone={reportTone[r.status]}>{t(`reports.statuses.${r.status}`)}</Badge>
                      <Badge>{t(`reports.categories.${r.category}`)}</Badge>
                      <span className="font-semibold">{r.title}</span>
                      <span className="text-muted">— {r.studentName} <span className="ltr-nums">{r.studentCode}</span></span>
                      <span className="ms-auto text-xs text-muted">{formatDateTime(r.createdAt, i18n.language)}</span>
                    </summary>
                    <p className="mt-3 whitespace-pre-wrap text-sm">{r.message}</p>
                    {r.reply && (
                      <div className="mt-3 rounded-lg bg-sunk p-3 text-sm">
                        <p className="mb-1 text-xs text-muted">{r.answeredByName} · {formatDateTime(r.answeredAt, i18n.language)}</p>
                        <p className="whitespace-pre-wrap">{r.reply}</p>
                      </div>
                    )}
                  </details>
                ))}
              </div>
              <Pager page={page} totalPages={data.totalPages ?? 1} onPage={setPage} />
            </>
          )
        }
      </QueryState>
    </>
  );
}

/** Requests to move a student's attendance check-in to a new phone. */
export function DevicesPage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [status, setStatus] = useState('pending');
  const [page, setPage] = useState(1);
  const requests = useQuery({
    queryKey: ['admin', 'device-changes', status, page],
    queryFn: () => call(api.GET('/api/admin/attendance/device-changes', { params: { query: { status: status || undefined, page, pageSize: 25 } } })),
  });
  const decide = useMutation({
    mutationFn: (v: { id: string; approve: boolean }) =>
      v.approve
        ? call(api.POST('/api/admin/attendance/device-changes/{id}/approve', { params: { path: { id: v.id } } }))
        : call(api.POST('/api/admin/attendance/device-changes/{id}/reject', { params: { path: { id: v.id } } })),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['admin', 'device-changes'] }),
  });

  return (
    <>
      <PageHeader title={t('admin.devices.title')} />
      <div className="mb-4 max-w-xs">
        <SelectField label={t('admin.common.status')} value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
          <option value="">{t('admin.common.all')}</option>
          {(['pending', 'approved', 'rejected'] as const).map((s) => <option key={s} value={s}>{t(`admin.devices.statuses.${s}`)}</option>)}
        </SelectField>
      </div>
      {decide.isError && <div className="mb-4"><Alert>{errorMessage(decide.error)}</Alert></div>}
      <QueryState query={requests}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('admin.devices.empty')}</EmptyState> : (
            <>
              <Table head={[t('admin.payments.student'), t('admin.devices.reason'), t('admin.activity.when'), t('admin.common.status'), '']}>
                {data.items.map((r) => (
                  <tr key={r.id}>
                    <td className="font-medium">{r.studentName}</td>
                    <td className="max-w-xs whitespace-pre-wrap">{r.reason}</td>
                    <td className="text-xs">{formatDateTime(r.createdAt, i18n.language)}</td>
                    <td><Badge tone={deviceTone[r.status as keyof typeof deviceTone] ?? 'neutral'}>{t(`admin.devices.statuses.${r.status}`, { defaultValue: r.status })}</Badge></td>
                    <td className="whitespace-nowrap">
                      {r.status === 'pending' && (
                        <>
                          <Button variant="ghost" className="min-h-8 px-2" onClick={() => decide.mutate({ id: r.id, approve: true })}>{t('admin.devices.approve')}</Button>
                          <Button variant="ghost" className="min-h-8 px-2" onClick={() => decide.mutate({ id: r.id, approve: false })}>{t('admin.devices.reject')}</Button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </Table>
              <Pager page={page} totalPages={data.totalPages ?? 1} onPage={setPage} />
            </>
          )
        }
      </QueryState>
    </>
  );
}

/** The audit trail: who changed settings, accounts, grades, payments and timetables. */
export function ActivityPage() {
  const { t, i18n } = useTranslation();
  const [page, setPage] = useState(1);
  const activity = useQuery({ queryKey: ['admin', 'activity', page], queryFn: () => call(api.GET('/api/admin/activity', { params: { query: { page, pageSize: 50 } } })) });

  return (
    <>
      <PageHeader title={t('admin.activity.title')} />
      <QueryState query={activity}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('admin.activity.empty')}</EmptyState> : (
            <>
              <Table head={[t('admin.activity.when'), t('admin.activity.actor'), t('admin.activity.action'), '']}>
                {data.items.map((a) => (
                  <tr key={a.id}>
                    <td className="whitespace-nowrap text-xs">{formatDateTime(a.occurredAt, i18n.language)}</td>
                    <td>{a.actorName ?? '—'}</td>
                    <td className="ltr-nums text-xs">{a.action}</td>
                    <td dir="auto">{a.summary}</td>
                  </tr>
                ))}
              </Table>
              <Pager page={page} totalPages={data.totalPages ?? 1} onPage={setPage} />
            </>
          )
        }
      </QueryState>
    </>
  );
}
