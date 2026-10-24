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
import { Alert, QueryState } from '../../components/ui/States';
import { Table } from '../../components/ui/Table';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { localName } from '../../i18n';
import { useDepartments, useFaculties } from './lookups';

const systems = ['credit_hours', 'year_groups'] as const;

/** Faculties and departments, and which academic system each one runs. */
export function OrganizationPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['lookup'] });
    void queryClient.invalidateQueries({ queryKey: ['admin', 'academic-systems'] });
  };

  return (
    <>
      <PageHeader title={t('admin.nav.organization')} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      <div className="grid gap-6">
        <Faculties onDone={refresh} onMessage={setMessage} />
        <Departments onDone={refresh} onMessage={setMessage} />
        <AcademicSystems onMessage={setMessage} />
      </div>
    </>
  );
}

type Notify = (message: { tone: 'success' | 'danger'; text: string }) => void;

function Faculties({ onDone, onMessage }: { onDone: () => void; onMessage: Notify }) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const faculties = useFaculties();
  const [editing, setEditing] = useState<Schema<'FacultyDto'> | 'new' | null>(null);
  const [form, setForm] = useState({ code: '', name: '', nameAr: '', isActive: true });

  const save = useMutation({
    mutationFn: () =>
      editing === 'new'
        ? call(api.POST('/api/admin/faculties', { body: { code: form.code, name: form.name, nameAr: form.nameAr } }))
        : call(api.PUT('/api/admin/faculties/{id}', { params: { path: { id: (editing as Schema<'FacultyDto'>).id } }, body: form })),
    onSuccess: () => { onDone(); setEditing(null); onMessage({ tone: 'success', text: t('admin.common.saved') }); },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function open(item: Schema<'FacultyDto'> | 'new') {
    setEditing(item);
    setForm(item === 'new' ? { code: '', name: '', nameAr: '', isActive: true } : { code: item.code, name: item.name, nameAr: item.nameAr, isActive: item.isActive });
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  return (
    <Card title={t('admin.org.faculties')} actions={<Button variant="secondary" onClick={() => open('new')}>{t('admin.org.newFaculty')}</Button>}>
      {editing && (
        <form className="mb-4 grid gap-3 sm:grid-cols-4" onSubmit={submit}>
          <TextField label={t('admin.common.code')} dir="ltr" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} required />
          <TextField label={t('admin.common.nameAr')} value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} required />
          <TextField label={t('admin.common.name')} dir="ltr" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          {editing !== 'new' && (
            <SelectField label={t('admin.common.status')} value={form.isActive ? '1' : '0'} onChange={(e) => setForm({ ...form, isActive: e.target.value === '1' })}>
              <option value="1">{t('admin.common.active')}</option>
              <option value="0">{t('admin.common.inactive')}</option>
            </SelectField>
          )}
          <div className="flex gap-2 sm:col-span-4">
            <Button type="submit" busy={save.isPending}>{t('admin.common.save')}</Button>
            <Button type="button" variant="ghost" onClick={() => setEditing(null)}>{t('app.cancel')}</Button>
          </div>
        </form>
      )}
      <QueryState query={faculties}>
        {(data) => (
          <Table head={[t('admin.common.code'), t('admin.common.nameAr'), t('admin.common.name'), t('admin.common.status'), '']}>
            {data.items.map((f) => (
              <tr key={f.id}>
                <td className="ltr-nums">{f.code}</td>
                <td>{f.nameAr}</td>
                <td dir="ltr" className="text-start">{f.name}</td>
                <td><Badge tone={f.isActive ? 'success' : 'neutral'}>{f.isActive ? t('admin.common.active') : t('admin.common.inactive')}</Badge></td>
                <td><Button variant="ghost" className="min-h-8 px-2" onClick={() => open(f)}>{t('admin.common.edit')}</Button></td>
              </tr>
            ))}
          </Table>
        )}
      </QueryState>
    </Card>
  );
}

function Departments({ onDone, onMessage }: { onDone: () => void; onMessage: Notify }) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const faculties = useFaculties();
  const departments = useDepartments();
  const [editing, setEditing] = useState<Schema<'DepartmentDto'> | 'new' | null>(null);
  const empty = { facultyId: '', code: '', name: '', nameAr: '', description: '', isActive: true };
  const [form, setForm] = useState(empty);

  const save = useMutation({
    mutationFn: () => {
      const body = { facultyId: form.facultyId, code: form.code, name: form.name, nameAr: form.nameAr, description: form.description || null };
      return editing === 'new'
        ? call(api.POST('/api/admin/departments', { body }))
        : call(api.PUT('/api/admin/departments/{id}', { params: { path: { id: (editing as Schema<'DepartmentDto'>).id } }, body: { ...body, isActive: form.isActive } }));
    },
    onSuccess: () => { onDone(); setEditing(null); onMessage({ tone: 'success', text: t('admin.common.saved') }); },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function open(item: Schema<'DepartmentDto'> | 'new') {
    setEditing(item);
    setForm(item === 'new' ? empty : { facultyId: item.facultyId, code: item.code, name: item.name, nameAr: item.nameAr, description: item.description ?? '', isActive: item.isActive });
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  return (
    <Card title={t('admin.org.departments')} actions={<Button variant="secondary" onClick={() => open('new')}>{t('admin.org.newDepartment')}</Button>}>
      {editing && (
        <form className="mb-4 grid gap-3 sm:grid-cols-3" onSubmit={submit}>
          <SelectField label={t('admin.common.faculty')} value={form.facultyId} onChange={(e) => setForm({ ...form, facultyId: e.target.value })} required>
            <option value="">{t('admin.common.select')}</option>
            {(faculties.data?.items ?? []).map((f) => <option key={f.id} value={f.id}>{localName(f, i18n.language)}</option>)}
          </SelectField>
          <TextField label={t('admin.common.code')} dir="ltr" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} required />
          <TextField label={t('admin.common.nameAr')} value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} required />
          <TextField label={t('admin.common.name')} dir="ltr" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          <TextField label={t('admin.common.description')} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          {editing !== 'new' && (
            <SelectField label={t('admin.common.status')} value={form.isActive ? '1' : '0'} onChange={(e) => setForm({ ...form, isActive: e.target.value === '1' })}>
              <option value="1">{t('admin.common.active')}</option>
              <option value="0">{t('admin.common.inactive')}</option>
            </SelectField>
          )}
          <div className="flex gap-2 sm:col-span-3">
            <Button type="submit" busy={save.isPending}>{t('admin.common.save')}</Button>
            <Button type="button" variant="ghost" onClick={() => setEditing(null)}>{t('app.cancel')}</Button>
          </div>
        </form>
      )}
      <QueryState query={departments}>
        {(data) => (
          <Table head={[t('admin.common.faculty'), t('admin.common.code'), t('admin.common.nameAr'), t('admin.common.name'), t('admin.common.status'), '']}>
            {data.items.map((d) => (
              <tr key={d.id}>
                <td className="ltr-nums">{d.facultyCode}</td>
                <td className="ltr-nums">{d.code}</td>
                <td>{d.nameAr}</td>
                <td dir="ltr" className="text-start">{d.name}</td>
                <td><Badge tone={d.isActive ? 'success' : 'neutral'}>{d.isActive ? t('admin.common.active') : t('admin.common.inactive')}</Badge></td>
                <td><Button variant="ghost" className="min-h-8 px-2" onClick={() => open(d)}>{t('admin.common.edit')}</Button></td>
              </tr>
            ))}
          </Table>
        )}
      </QueryState>
    </Card>
  );
}

/** One select per level; "inherit" clears the override. Refused (409) while affected students have history. */
function AcademicSystems({ onMessage }: { onMessage: Notify }) {
  const { t, i18n } = useTranslation();
  const errorMessage = useErrorMessage();
  const queryClient = useQueryClient();
  const overview = useQuery({ queryKey: ['admin', 'academic-systems'], queryFn: () => call(api.GET('/api/admin/academic-systems')) });

  const set = useMutation({
    mutationFn: (v: { level: 'university' | 'faculty' | 'department'; id?: string; value: string | null }) => {
      const body = { academicSystem: v.value };
      if (v.level === 'university') return call(api.PUT('/api/admin/academic-system', { body }));
      if (v.level === 'faculty') return call(api.PUT('/api/admin/faculties/{facultyId}/academic-system', { params: { path: { facultyId: v.id! } }, body }));
      return call(api.PUT('/api/admin/departments/{departmentId}/academic-system', { params: { path: { departmentId: v.id! } }, body }));
    },
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['admin', 'academic-systems'] }); onMessage({ tone: 'success', text: t('admin.org.systemSaved') }); },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  const label = (system: string) => t(`profile.systems.${system}`);

  return (
    <Card title={t('admin.org.systems')}>
      <QueryState query={overview}>
        {(data) => (
          <div className="grid gap-4">
            <div className="max-w-xs">
              <SelectField label={t('admin.org.universityDefault')} value={data.university.academicSystem} onChange={(e) => set.mutate({ level: 'university', value: e.target.value })}>
                {systems.map((s) => <option key={s} value={s}>{label(s)}</option>)}
              </SelectField>
            </div>
            <Table head={[t('admin.common.faculty'), t('admin.org.systems'), t('admin.org.effective')]}>
              {data.faculties.map((f) => (
                <tr key={f.facultyId}>
                  <td>{localName(f, i18n.language)}</td>
                  <td>
                    <select aria-label={localName(f, i18n.language)} className="min-h-9 rounded-lg border border-line bg-surface px-2 text-sm" value={f.override ?? ''} onChange={(e) => set.mutate({ level: 'faculty', id: f.facultyId, value: e.target.value || null })}>
                      <option value="">{t('admin.org.inherit')}</option>
                      {systems.map((s) => <option key={s} value={s}>{label(s)}</option>)}
                    </select>
                  </td>
                  <td><Badge tone="primary">{label(f.effective)}</Badge></td>
                </tr>
              ))}
            </Table>
            <Table head={[t('admin.common.department'), t('admin.org.systems'), t('admin.org.effective')]}>
              {data.departments.map((d) => (
                <tr key={d.departmentId}>
                  <td><span className="ltr-nums">{d.code}</span> — {localName(d, i18n.language)}</td>
                  <td>
                    <select aria-label={localName(d, i18n.language)} className="min-h-9 rounded-lg border border-line bg-surface px-2 text-sm" value={d.override ?? ''} onChange={(e) => set.mutate({ level: 'department', id: d.departmentId, value: e.target.value || null })}>
                      <option value="">{t('admin.org.inherit')}</option>
                      {systems.map((s) => <option key={s} value={s}>{label(s)}</option>)}
                    </select>
                  </td>
                  <td><Badge tone="primary">{label(d.effective)}</Badge></td>
                </tr>
              ))}
            </Table>
          </div>
        )}
      </QueryState>
    </Card>
  );
}
