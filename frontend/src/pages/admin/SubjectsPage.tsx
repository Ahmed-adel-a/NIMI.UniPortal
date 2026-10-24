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
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { Table } from '../../components/ui/Table';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { localName } from '../../i18n';
import { useCurrentTerm, useDepartments, useStaff } from './lookups';

type Subject = Schema<'SubjectDto'>;
type Notify = (m: { tone: 'success' | 'danger'; text: string }) => void;

/** The subject catalog per department; a picked subject shows its groups and teaching staff for the active term. */
export function SubjectsPage() {
  const { t, i18n } = useTranslation();
  const departments = useDepartments();
  const [departmentId, setDepartmentId] = useState('');
  const [search, setSearch] = useState('');
  const [selected, setSelected] = useState<Subject | null>(null);
  const [editing, setEditing] = useState<Subject | 'new' | null>(null);
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);

  const subjects = useQuery({
    queryKey: ['admin', 'subjects', departmentId, search],
    queryFn: () => call(api.GET('/api/subjects', { params: { query: { departmentId: departmentId || undefined, search: search || undefined, includeInactive: true, pageSize: 100 } } })),
  });

  return (
    <>
      <PageHeader title={t('admin.subjects.title')} actions={<Button onClick={() => setEditing('new')}>{t('admin.subjects.new')}</Button>} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      {editing && <SubjectForm subject={editing} defaultDepartmentId={departmentId} onClose={() => setEditing(null)} onMessage={setMessage} />}
      <div className="grid gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
        <Card>
          <div className="mb-3 grid gap-3 sm:grid-cols-2">
            <SelectField label={t('admin.common.department')} value={departmentId} onChange={(e) => setDepartmentId(e.target.value)}>
              <option value="">{t('admin.common.all')}</option>
              {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
            </SelectField>
            <TextField label={t('admin.common.search')} value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
          <QueryState query={subjects}>
            {(data) =>
              data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
                <ul className="divide-y divide-line">
                  {data.items.map((s) => (
                    <li key={s.id}>
                      <button
                        type="button"
                        onClick={() => setSelected(s)}
                        className={`flex w-full items-center justify-between gap-3 px-2 py-2.5 text-start text-sm hover:bg-sunk ${selected?.id === s.id ? 'bg-sunk' : ''}`}
                      >
                        <span>
                          <span className="font-semibold ltr-nums">{s.code}</span> — {localName(s, i18n.language)}
                          {!s.isActive && <span className="ms-2"><Badge>{t('admin.common.inactive')}</Badge></span>}
                        </span>
                        <span className="text-xs text-muted ltr-nums">{s.creditHours}</span>
                      </button>
                    </li>
                  ))}
                </ul>
              )
            }
          </QueryState>
        </Card>
        {selected ? (
          <div className="grid content-start gap-6">
            <Card
              title={<><span className="ltr-nums">{selected.code}</span> — {localName(selected, i18n.language)}</>}
              actions={<Button variant="secondary" onClick={() => setEditing(selected)}>{t('admin.common.edit')}</Button>}
            >
              <dl className="grid grid-cols-2 gap-2 text-sm sm:grid-cols-4">
                <div><dt className="text-muted">{t('admin.subjects.credits')}</dt><dd className="ltr-nums">{selected.creditHours}</dd></div>
                <div><dt className="text-muted">{t('admin.subjects.category')}</dt><dd>{t(`admin.subjects.categories.${selected.category}`)}</dd></div>
                <div><dt className="text-muted">{t('admin.subjects.requirement')}</dt><dd>{selected.isGraduationRequirement ? t('app.yes') : t('app.no')}</dd></div>
                <div><dt className="text-muted">{t('admin.common.status')}</dt><dd>{selected.isActive ? t('admin.common.active') : t('admin.common.inactive')}</dd></div>
              </dl>
            </Card>
            <Groups subject={selected} onMessage={setMessage} />
            <Teachers subject={selected} onMessage={setMessage} />
          </div>
        ) : (
          <EmptyState>{t('admin.subjects.pick')}</EmptyState>
        )}
      </div>
    </>
  );
}

function SubjectForm({ subject, defaultDepartmentId, onClose, onMessage }: { subject: Subject | 'new'; defaultDepartmentId: string; onClose: () => void; onMessage: Notify }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const departments = useDepartments();
  const isNew = subject === 'new';
  const [form, setForm] = useState(
    isNew
      ? { departmentId: defaultDepartmentId, code: '', name: '', nameAr: '', description: '', creditHours: '3', category: 'regular' as Schema<'SubjectCategory'>, isGraduationRequirement: false, isActive: true }
      : { departmentId: subject.departmentId, code: subject.code, name: subject.name, nameAr: subject.nameAr, description: subject.description ?? '', creditHours: String(subject.creditHours), category: subject.category, isGraduationRequirement: subject.isGraduationRequirement, isActive: subject.isActive },
  );

  const save = useMutation({
    mutationFn: () => {
      const common = { name: form.name, nameAr: form.nameAr, description: form.description || null, creditHours: Number(form.creditHours), category: form.category, isGraduationRequirement: form.isGraduationRequirement };
      return isNew
        ? call(api.POST('/api/admin/subjects', { body: { ...common, departmentId: form.departmentId, code: form.code } }))
        : call(api.PUT('/api/admin/subjects/{id}', { params: { path: { id: subject.id } }, body: { ...common, isActive: form.isActive } }));
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'subjects'] });
      onClose();
      onMessage({ tone: 'success', text: t('admin.common.saved') });
    },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  return (
    <Card title={isNew ? t('admin.subjects.new') : subject.code} className="mb-6">
      <form className="grid gap-3 sm:grid-cols-3" onSubmit={submit}>
        <SelectField label={t('admin.common.department')} value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value })} disabled={!isNew} required>
          <option value="">{t('admin.common.select')}</option>
          {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
        </SelectField>
        <TextField label={t('admin.common.code')} dir="ltr" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} disabled={!isNew} required />
        <TextField label={t('admin.subjects.credits')} type="number" dir="ltr" value={form.creditHours} onChange={(e) => setForm({ ...form, creditHours: e.target.value })} required />
        <TextField label={t('admin.common.nameAr')} value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} required />
        <TextField label={t('admin.common.name')} dir="ltr" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
        <SelectField label={t('admin.subjects.category')} value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value as Schema<'SubjectCategory'> })}>
          <option value="regular">{t('admin.subjects.categories.regular')}</option>
          <option value="military_education">{t('admin.subjects.categories.military_education')}</option>
        </SelectField>
        <TextField label={t('admin.common.description')} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
        <label className="flex items-center gap-2 self-end pb-2 text-sm">
          <input type="checkbox" checked={form.isGraduationRequirement} onChange={(e) => setForm({ ...form, isGraduationRequirement: e.target.checked })} />
          {t('admin.subjects.requirement')}
        </label>
        {!isNew && (
          <label className="flex items-center gap-2 self-end pb-2 text-sm">
            <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} />
            {t('admin.common.active')}
          </label>
        )}
        <div className="flex gap-2 sm:col-span-3">
          <Button type="submit" busy={save.isPending}>{t('admin.common.save')}</Button>
          <Button type="button" variant="ghost" onClick={onClose}>{t('app.cancel')}</Button>
        </div>
      </form>
    </Card>
  );
}

function Groups({ subject, onMessage }: { subject: Subject; onMessage: Notify }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const term = useCurrentTerm();
  const departments = useDepartments();
  const termId = term.data?.id;
  const groups = useQuery({
    queryKey: ['admin', 'groups', subject.id, termId],
    queryFn: () => call(api.GET('/api/subjects/{subjectId}/groups', { params: { path: { subjectId: subject.id }, query: { termId, pageSize: 100 } } })),
    enabled: !!termId,
  });
  const [form, setForm] = useState({ groupNumber: '', capacity: '40', targetDepartmentId: '' });

  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/subjects/{subjectId}/groups', {
        params: { path: { subjectId: subject.id } },
        body: { termId: termId!, groupNumber: Number(form.groupNumber), capacity: Number(form.capacity), targetDepartmentId: form.targetDepartmentId || null },
      })),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'groups', subject.id] });
      setForm({ ...form, groupNumber: '' });
      onMessage({ tone: 'success', text: t('admin.common.saved') });
    },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  if (!term.isPending && !termId) return <Card title={t('admin.subjects.groups')}><EmptyState>{t('admin.common.noActiveTerm')}</EmptyState></Card>;

  return (
    <Card title={t('admin.subjects.groups')}>
      <QueryState query={groups}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <Table head={[t('admin.subjects.groupNumber'), t('admin.subjects.capacity'), t('admin.common.department')]}>
              {data.items.map((g) => (
                <tr key={g.id}>
                  <td className="ltr-nums">{g.groupNumber}</td>
                  <td className="ltr-nums">{g.capacity}</td>
                  <td>{g.targetDepartmentId ? localName(departments.data?.items.find((d) => d.id === g.targetDepartmentId), i18n.language) : t('admin.common.all')}</td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
      <form className="mt-4 grid gap-3 border-t border-line pt-4 sm:grid-cols-4" onSubmit={submit}>
        <TextField label={t('admin.subjects.groupNumber')} type="number" dir="ltr" value={form.groupNumber} onChange={(e) => setForm({ ...form, groupNumber: e.target.value })} required />
        <TextField label={t('admin.subjects.capacity')} type="number" dir="ltr" value={form.capacity} onChange={(e) => setForm({ ...form, capacity: e.target.value })} required />
        <SelectField label={t('admin.common.department')} value={form.targetDepartmentId} onChange={(e) => setForm({ ...form, targetDepartmentId: e.target.value })}>
          <option value="">{t('admin.common.all')}</option>
          {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code}</option>)}
        </SelectField>
        <div className="flex items-end"><Button type="submit" busy={create.isPending}>{t('admin.subjects.newGroup')}</Button></div>
      </form>
    </Card>
  );
}

function Teachers({ subject, onMessage }: { subject: Subject; onMessage: Notify }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const term = useCurrentTerm();
  const staff = useStaff();
  const termId = term.data?.id;
  const assignments = useQuery({
    queryKey: ['admin', 'teaching', subject.id, termId],
    queryFn: () => call(api.GET('/api/admin/teaching-assignments', { params: { query: { subjectId: subject.id, termId, pageSize: 100 } } })),
    enabled: !!termId,
  });
  const groups = useQuery({
    queryKey: ['admin', 'groups', subject.id, termId],
    queryFn: () => call(api.GET('/api/subjects/{subjectId}/groups', { params: { path: { subjectId: subject.id }, query: { termId, pageSize: 100 } } })),
    enabled: !!termId,
  });
  const [form, setForm] = useState({ staffId: '', subjectGroupId: '', role: 'lecturer' as Schema<'TeachingRole'> });
  const refresh = () => void queryClient.invalidateQueries({ queryKey: ['admin', 'teaching', subject.id] });
  const onError = (e: unknown) => onMessage({ tone: 'danger', text: errorMessage(e) });

  const assign = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/teaching-assignments', {
        body: { staffId: form.staffId, subjectId: subject.id, termId: termId!, subjectGroupId: form.subjectGroupId || null, role: form.role },
      })),
    onSuccess: () => { refresh(); onMessage({ tone: 'success', text: t('admin.common.saved') }); },
    onError,
  });
  const remove = useMutation({
    mutationFn: (id: string) => call(api.DELETE('/api/admin/teaching-assignments/{id}', { params: { path: { id } } })),
    onSuccess: refresh,
    onError,
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    assign.mutate();
  }

  if (!term.isPending && !termId) return null;

  return (
    <Card title={t('admin.subjects.teachers')}>
      <QueryState query={assignments}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <Table head={[t('admin.subjects.staff'), t('admin.timetable.group'), t('admin.subjects.teachingRole'), '']}>
              {data.items.map((a) => (
                <tr key={a.id}>
                  <td>{a.staffName}</td>
                  <td className="ltr-nums">{a.groupNumber ?? t('admin.subjects.wholeSubject')}</td>
                  <td>{t(`admin.subjects.roles.${a.role}`)}</td>
                  <td><Button variant="ghost" className="min-h-8 px-2" onClick={() => remove.mutate(a.id)}>{t('admin.subjects.remove')}</Button></td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
      <form className="mt-4 grid gap-3 border-t border-line pt-4 sm:grid-cols-4" onSubmit={submit}>
        <SelectField label={t('admin.subjects.staff')} value={form.staffId} onChange={(e) => setForm({ ...form, staffId: e.target.value })} required>
          <option value="">{t('admin.common.select')}</option>
          {(staff.data ?? []).map((s) => <option key={s.id} value={s.id}>{s.fullName}</option>)}
        </SelectField>
        <SelectField label={t('admin.timetable.group')} value={form.subjectGroupId} onChange={(e) => setForm({ ...form, subjectGroupId: e.target.value })}>
          <option value="">{t('admin.subjects.wholeSubject')}</option>
          {(groups.data?.items ?? []).map((g) => <option key={g.id} value={g.id}>{g.groupNumber}</option>)}
        </SelectField>
        <SelectField label={t('admin.subjects.teachingRole')} value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as Schema<'TeachingRole'> })}>
          {(['lecturer', 'lab', 'tutorial'] as const).map((r) => <option key={r} value={r}>{t(`admin.subjects.roles.${r}`)}</option>)}
        </SelectField>
        <div className="flex items-end"><Button type="submit" busy={assign.isPending}>{t('admin.subjects.assign')}</Button></div>
      </form>
    </Card>
  );
}
