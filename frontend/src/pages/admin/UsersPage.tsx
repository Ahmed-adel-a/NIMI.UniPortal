import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, sendForm } from '../../api/client';
import type { Schema } from '../../api/types';
import type { Role } from '../../auth/AuthContext';
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
import { formatDateTime } from '../../lib/format';
import { useDepartments } from './lookups';

const roles: Role[] = ['student', 'professor', 'department_head', 'admin'];

export function UsersPage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [role, setRole] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const departments = useDepartments();

  const users = useQuery({
    queryKey: ['admin', 'users', role, departmentId, search, page],
    queryFn: () =>
      call(api.GET('/api/admin/users', {
        params: { query: { role: role || undefined, departmentId: departmentId || undefined, search: search || undefined, page, pageSize: 25 } },
      })),
  });
  const refresh = () => void queryClient.invalidateQueries({ queryKey: ['admin', 'users'] });

  const setStatus = useMutation({
    mutationFn: (v: { id: string; isActive: boolean }) => call(api.PATCH('/api/admin/users/{id}/status', { params: { path: { id: v.id } }, body: { isActive: v.isActive } })),
    onSuccess: () => { refresh(); setMessage(null); },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });
  const reset = useMutation({
    mutationFn: (id: string) => call(api.POST('/api/admin/users/{id}/reset-password', { params: { path: { id } } })),
    onSuccess: (r) => setMessage({ tone: 'success', text: t('admin.users.tempPassword', { password: r.temporaryPassword }) }),
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });
  const importCsv = useMutation({
    mutationFn: (file: File) => {
      const form = new FormData();
      form.append('file', file);
      return sendForm<Schema<'ImportStudentsReport'>>('/api/admin/users/bulk', form);
    },
    onSuccess: (r) => {
      refresh();
      const errors = r.rows.filter((row) => row.error).map((row) => `#${row.row}: ${row.error}`).join(' · ');
      setMessage({ tone: r.failed > 0 ? 'danger' : 'success', text: `${t('admin.users.imported', { created: r.created, skipped: r.skipped, failed: r.failed })}${errors ? ` — ${errors}` : ''}` });
    },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  return (
    <>
      <PageHeader
        title={t('admin.users.title')}
        actions={
          <>
            <Button onClick={() => setCreating(!creating)}>{t('admin.users.new')}</Button>
            <label className="inline-flex min-h-10 cursor-pointer items-center rounded-lg border border-line bg-surface px-4 text-sm font-semibold hover:bg-sunk" title={t('admin.users.importHint')}>
              {t('admin.users.import')}
              <input type="file" accept=".csv,text/csv" className="sr-only" onChange={(e) => { const f = e.target.files?.[0]; if (f) importCsv.mutate(f); e.target.value = ''; }} />
            </label>
          </>
        }
      />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      {creating && <CreateUser onCreated={(text) => { setMessage({ tone: 'success', text }); refresh(); setCreating(false); }} />}

      <div className="mb-4 grid gap-3 sm:grid-cols-3">
        <SelectField label={t('admin.users.role')} value={role} onChange={(e) => { setRole(e.target.value); setPage(1); }}>
          <option value="">{t('admin.common.all')}</option>
          {roles.map((r) => <option key={r} value={r}>{t(`roles.${r}`)}</option>)}
        </SelectField>
        <SelectField label={t('admin.common.department')} value={departmentId} onChange={(e) => { setDepartmentId(e.target.value); setPage(1); }}>
          <option value="">{t('admin.common.all')}</option>
          {(departments.data?.items ?? []).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
        </SelectField>
        <TextField label={t('admin.common.search')} value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
      </div>

      <QueryState query={users}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('app.none')}</EmptyState>
          ) : (
            <>
              <Table head={[t('profile.fullName'), t('admin.users.role'), t('admin.users.code'), t('admin.common.department'), t('admin.users.lastLogin'), t('admin.common.status'), '']}>
                {data.items.map((u) => (
                  <tr key={u.id}>
                    <td>
                      <span className="font-medium">{u.fullName}</span>
                      <span className="block text-xs text-muted ltr-nums">{u.email}</span>
                    </td>
                    <td>{t(`roles.${u.role}`)}</td>
                    <td className="ltr-nums">{u.studentCode ?? '—'}</td>
                    <td className="ltr-nums">{u.departmentCode ? `${u.facultyCode}/${u.departmentCode}` : '—'}</td>
                    <td className="text-xs">{formatDateTime(u.lastLoginAt, i18n.language) || '—'}</td>
                    <td><Badge tone={u.isActive ? 'success' : 'neutral'}>{u.isActive ? t('admin.common.active') : t('admin.common.inactive')}</Badge></td>
                    <td className="whitespace-nowrap">
                      <Button variant="ghost" className="min-h-8 px-2" onClick={() => setStatus.mutate({ id: u.id, isActive: !u.isActive })}>
                        {u.isActive ? t('admin.common.deactivate') : t('admin.common.activate')}
                      </Button>
                      <Button variant="ghost" className="min-h-8 px-2" busy={reset.isPending && reset.variables === u.id} onClick={() => reset.mutate(u.id)}>
                        {t('admin.users.reset')}
                      </Button>
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

function CreateUser({ onCreated }: { onCreated: (message: string) => void }) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const departments = useDepartments();
  const [form, setForm] = useState({ role: 'student' as Role, fullName: '', email: '', password: '', departmentId: '', enrollmentYear: String(new Date().getFullYear()), academicTitle: 'lecturer', nationalId: '', gender: '', phone: '' });
  const set = (patch: Partial<typeof form>) => setForm({ ...form, ...patch });
  const needsDepartment = form.role !== 'admin';

  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/users', {
        body: {
          role: form.role,
          fullName: form.fullName,
          email: form.email,
          password: form.password || null,
          departmentId: needsDepartment ? form.departmentId : null,
          enrollmentYear: form.role === 'student' ? Number(form.enrollmentYear) : null,
          academicTitle: form.role === 'professor' || form.role === 'department_head' ? (form.academicTitle as 'professor' | 'lecturer' | 'demonstrator') : null,
          nationalId: form.nationalId || null,
          dateOfBirth: null,
          gender: (form.gender || null) as 'male' | 'female' | null,
          phone: form.phone || null,
        },
      })),
    onSuccess: (r) => onCreated(r.temporaryPassword ? `${t('admin.users.created')} ${t('admin.users.tempPassword', { password: r.temporaryPassword })}` : t('admin.users.created')),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <Card title={t('admin.users.new')} className="mb-6">
      <form className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3" onSubmit={submit}>
        <SelectField label={t('admin.users.role')} value={form.role} onChange={(e) => set({ role: e.target.value as Role })}>
          {roles.map((r) => <option key={r} value={r}>{t(`roles.${r}`)}</option>)}
        </SelectField>
        <TextField label={t('profile.fullName')} value={form.fullName} onChange={(e) => set({ fullName: e.target.value })} required />
        <TextField label={t('admin.users.email')} type="email" dir="ltr" value={form.email} onChange={(e) => set({ email: e.target.value })} required />
        {needsDepartment && (
          <SelectField label={t('admin.common.department')} value={form.departmentId} onChange={(e) => set({ departmentId: e.target.value })} required>
            <option value="">{t('admin.common.select')}</option>
            {(departments.data?.items ?? []).filter((d) => d.isActive).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code} — {localName(d, i18n.language)}</option>)}
          </SelectField>
        )}
        {form.role === 'student' && <TextField label={t('admin.users.enrollmentYear')} type="number" dir="ltr" value={form.enrollmentYear} onChange={(e) => set({ enrollmentYear: e.target.value })} required />}
        {(form.role === 'professor' || form.role === 'department_head') && (
          <SelectField label={t('admin.users.title2')} value={form.academicTitle} onChange={(e) => set({ academicTitle: e.target.value })}>
            {(['professor', 'lecturer', 'demonstrator'] as const).map((x) => <option key={x} value={x}>{t(`admin.users.titles.${x}`)}</option>)}
          </SelectField>
        )}
        <TextField label={t('admin.users.nationalId')} dir="ltr" inputMode="numeric" value={form.nationalId} onChange={(e) => set({ nationalId: e.target.value })} />
        <SelectField label={t('admin.users.gender')} value={form.gender} onChange={(e) => set({ gender: e.target.value })}>
          <option value="">—</option>
          <option value="male">{t('admin.users.genders.male')}</option>
          <option value="female">{t('admin.users.genders.female')}</option>
        </SelectField>
        <TextField label={t('admin.users.phone')} dir="ltr" value={form.phone} onChange={(e) => set({ phone: e.target.value })} />
        <TextField label={t('admin.users.password')} type="password" dir="ltr" value={form.password} onChange={(e) => set({ password: e.target.value })} hint={t('admin.users.passwordHint')} autoComplete="new-password" />
        <div className="flex items-end sm:col-span-2 lg:col-span-3">
          <Button type="submit" busy={create.isPending}>{t('admin.common.create')}</Button>
        </div>
        {create.isError && <div className="sm:col-span-2 lg:col-span-3"><Alert>{errorMessage(create.error)}</Alert></div>}
      </form>
    </Card>
  );
}
