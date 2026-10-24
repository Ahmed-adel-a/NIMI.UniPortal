import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import type { Schema } from '../../api/types';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { SelectField, TextField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Pager } from '../../components/ui/Pager';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { Table } from '../../components/ui/Table';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { localName } from '../../i18n';
import { formatDate, formatTime, jsDayOf } from '../../lib/format';
import { useCurrentTerm, useDepartments, useStaff } from './lookups';

/** The API's weekdays start from Saturday (0) to Friday (6). */
const productDays = [0, 1, 2, 3, 4, 5, 6];

/** The active term's timetable: slots per group, hall and lecturer; conflicts come back as 409. */
export function TimetablePage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const days: string[] = t('days', { returnObjects: true }) as unknown as string[];
  const term = useCurrentTerm();
  const departments = useDepartments();
  const staff = useStaff();
  const termId = term.data?.id;
  const [departmentId, setDepartmentId] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);

  const slots = useQuery({
    queryKey: ['admin', 'schedules', termId, departmentId, page],
    queryFn: () => call(api.GET('/api/admin/schedules', { params: { query: { termId, departmentId: departmentId || undefined, page, pageSize: 50 } } })),
    enabled: !!termId,
  });
  const cancel = useMutation({
    mutationFn: (id: string) => call(api.POST('/api/admin/schedules/{id}/cancel', { params: { path: { id } } })),
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['admin', 'schedules'] }); setMessage({ tone: 'success', text: t('admin.common.saved') }); },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  const staffName = (id: string | null | undefined) => (id ? staff.data?.find((s) => s.id === id)?.fullName ?? '—' : '—');

  if (!term.isPending && !termId) {
    return (
      <>
        <PageHeader title={t('admin.timetable.title')} />
        <EmptyState>{t('admin.common.noActiveTerm')}</EmptyState>
      </>
    );
  }

  return (
    <>
      <PageHeader title={t('admin.timetable.title')} actions={<Button onClick={() => setCreating(!creating)}>{t('admin.timetable.new')}</Button>} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      {creating && termId && (
        <SlotForm
          termId={termId}
          startsOn={term.data?.startsOn}
          onDone={(text) => { setMessage({ tone: 'success', text }); setCreating(false); void queryClient.invalidateQueries({ queryKey: ['admin', 'schedules'] }); }}
        />
      )}
      <div className="mb-4 max-w-sm">
        <SelectField label={t('admin.common.department')} value={departmentId} onChange={(e) => { setDepartmentId(e.target.value); setPage(1); }}>
          <option value="">{t('admin.common.all')}</option>
          {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
        </SelectField>
      </div>
      <QueryState query={slots}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <>
              <Table head={[t('admin.timetable.day'), t('admin.timetable.start'), t('admin.timetable.group'), t('admin.timetable.facility'), t('admin.subjects.staff'), t('admin.timetable.recurrence'), t('admin.timetable.until'), '']}>
                {data.items.map((s) => (
                  <tr key={s.id} className={s.isCancelled ? 'opacity-60' : ''}>
                    <td>{days[jsDayOf(s.dayOfWeek)]}</td>
                    <td className="ltr-nums">{formatTime(s.startTime)}–{formatTime(s.endTime)}</td>
                    <td><span className="ltr-nums">{s.subjectCode}</span> · {s.groupNumber}</td>
                    <td>{s.facilityName}</td>
                    <td>{staffName(s.staffId)}</td>
                    <td>{t(`admin.timetable.recurrences.${s.recurrence}`)}</td>
                    <td>{formatDate(s.validUntil, i18n.language)}</td>
                    <td>
                      {s.isCancelled ? (
                        <Badge>{t('admin.timetable.cancelled')}</Badge>
                      ) : (
                        <Button variant="ghost" className="min-h-8 px-2" onClick={() => window.confirm(t('admin.timetable.confirmCancel')) && cancel.mutate(s.id)}>
                          {t('admin.timetable.cancel')}
                        </Button>
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

function SlotForm({ termId, startsOn, onDone }: { termId: string; startsOn?: string; onDone: (message: string) => void }) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const days: string[] = t('days', { returnObjects: true }) as unknown as string[];
  const departments = useDepartments();
  const staff = useStaff();
  const [departmentId, setDepartmentId] = useState('');
  const [subjectId, setSubjectId] = useState('');
  const [form, setForm] = useState({
    subjectGroupId: '', facilityId: '', staffId: '', dayOfWeek: '0', startTime: '09:00', endTime: '11:00',
    recurrence: 'weekly' as Schema<'Recurrence'>, validFrom: startsOn ?? '', validUntil: '',
  });

  const subjects = useQuery({
    queryKey: ['admin', 'subjects', departmentId, ''],
    queryFn: () => call(api.GET('/api/subjects', { params: { query: { departmentId: departmentId || undefined, pageSize: 100 } } })),
  });
  const groups = useQuery({
    queryKey: ['admin', 'groups', subjectId, termId],
    queryFn: () => call(api.GET('/api/subjects/{subjectId}/groups', { params: { path: { subjectId }, query: { termId, pageSize: 100 } } })),
    enabled: !!subjectId,
  });
  const facilities = useQuery({ queryKey: ['admin', 'facilities', 'active'], queryFn: () => call(api.GET('/api/facilities', { params: { query: { pageSize: 100 } } })) });

  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/schedules', {
        body: {
          subjectGroupId: form.subjectGroupId,
          facilityId: form.facilityId,
          staffId: form.staffId || null,
          dayOfWeek: Number(form.dayOfWeek),
          startTime: `${form.startTime}:00`,
          endTime: `${form.endTime}:00`,
          recurrence: form.recurrence,
          validFrom: form.validFrom,
          validUntil: form.validUntil || null,
        },
      })),
    onSuccess: () => onDone(t('admin.common.saved')),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <Card title={t('admin.timetable.new')} className="mb-6">
      <form className="grid gap-3 sm:grid-cols-3 lg:grid-cols-4" onSubmit={submit}>
        <SelectField label={t('admin.common.department')} value={departmentId} onChange={(e) => { setDepartmentId(e.target.value); setSubjectId(''); }}>
          <option value="">{t('admin.common.all')}</option>
          {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
        </SelectField>
        <SelectField label={t('admin.subjects.title')} value={subjectId} onChange={(e) => { setSubjectId(e.target.value); setForm({ ...form, subjectGroupId: '' }); }} required>
          <option value="">{t('admin.common.select')}</option>
          {(subjects.data?.items ?? []).map((s) => <option key={s.id} value={s.id}>{s.code} — {localName(s, i18n.language)}</option>)}
        </SelectField>
        <SelectField label={t('admin.timetable.group')} value={form.subjectGroupId} onChange={(e) => setForm({ ...form, subjectGroupId: e.target.value })} required>
          <option value="">{t('admin.common.select')}</option>
          {(groups.data?.items ?? []).map((g) => <option key={g.id} value={g.id}>{g.groupNumber}</option>)}
        </SelectField>
        <SelectField label={t('admin.timetable.facility')} value={form.facilityId} onChange={(e) => setForm({ ...form, facilityId: e.target.value })} required>
          <option value="">{t('admin.common.select')}</option>
          {(facilities.data?.items ?? []).map((f) => <option key={f.id} value={f.id}>{f.name} ({f.capacity})</option>)}
        </SelectField>
        <SelectField label={t('admin.timetable.staff')} value={form.staffId} onChange={(e) => setForm({ ...form, staffId: e.target.value })}>
          <option value="">—</option>
          {(staff.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.fullName}</option>)}
        </SelectField>
        <SelectField label={t('admin.timetable.day')} value={form.dayOfWeek} onChange={(e) => setForm({ ...form, dayOfWeek: e.target.value })}>
          {productDays.map((d) => <option key={d} value={d}>{days[jsDayOf(d)]}</option>)}
        </SelectField>
        <TextField label={t('admin.timetable.start')} type="time" value={form.startTime} onChange={(e) => setForm({ ...form, startTime: e.target.value })} required />
        <TextField label={t('admin.timetable.end')} type="time" value={form.endTime} onChange={(e) => setForm({ ...form, endTime: e.target.value })} required />
        <SelectField label={t('admin.timetable.recurrence')} value={form.recurrence} onChange={(e) => setForm({ ...form, recurrence: e.target.value as Schema<'Recurrence'> })}>
          {(['weekly', 'biweekly', 'once'] as const).map((r) => <option key={r} value={r}>{t(`admin.timetable.recurrences.${r}`)}</option>)}
        </SelectField>
        <TextField label={t('admin.timetable.from')} type="date" value={form.validFrom} onChange={(e) => setForm({ ...form, validFrom: e.target.value })} required />
        <TextField label={t('admin.timetable.until')} type="date" value={form.validUntil} onChange={(e) => setForm({ ...form, validUntil: e.target.value })} hint={t('admin.timetable.untilHint')} />
        <div className="flex items-end"><Button type="submit" busy={create.isPending}>{t('admin.common.create')}</Button></div>
        {create.isError && <div className="sm:col-span-3 lg:col-span-4"><Alert>{errorMessage(create.error)}</Alert></div>}
      </form>
    </Card>
  );
}
