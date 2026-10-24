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
import { useFaculties } from './lookups';

type FacilityType = Schema<'FacilityType'>;
const types: FacilityType[] = ['lecture_hall', 'lab', 'auditorium', 'classroom', 'workshop'];

/** Halls and labs per faculty, or shared by the whole university. */
export function FacilitiesPage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const faculties = useFaculties();
  const facilities = useQuery({ queryKey: ['admin', 'facilities'], queryFn: () => call(api.GET('/api/facilities', { params: { query: { includeInactive: true, pageSize: 100 } } })) });
  const [editing, setEditing] = useState<Schema<'FacilityDto'> | 'new' | null>(null);
  const empty = { facultyId: '', name: '', type: 'lecture_hall' as FacilityType, capacity: '60', building: '', floor: '', isActive: true };
  const [form, setForm] = useState(empty);
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);

  const save = useMutation({
    mutationFn: () => {
      const body = { facultyId: form.facultyId || null, name: form.name, type: form.type, capacity: Number(form.capacity), building: form.building || null, floor: form.floor || null };
      return editing === 'new'
        ? call(api.POST('/api/admin/facilities', { body }))
        : call(api.PUT('/api/admin/facilities/{id}', { params: { path: { id: (editing as Schema<'FacilityDto'>).id } }, body: { ...body, isActive: form.isActive } }));
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'facilities'] });
      setEditing(null);
      setMessage({ tone: 'success', text: t('admin.common.saved') });
    },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function open(item: Schema<'FacilityDto'> | 'new') {
    setEditing(item);
    setForm(item === 'new' ? empty : { facultyId: item.facultyId ?? '', name: item.name, type: item.type, capacity: String(item.capacity), building: item.building ?? '', floor: item.floor ?? '', isActive: item.isActive });
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  const facultyName = (id: string | null | undefined) => (id ? localName(faculties.data?.items.find((f) => f.id === id), i18n.language) : t('admin.facilities.shared'));

  return (
    <>
      <PageHeader title={t('admin.facilities.title')} actions={<Button onClick={() => open('new')}>{t('admin.facilities.new')}</Button>} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      {editing && (
        <Card title={editing === 'new' ? t('admin.facilities.new') : editing.name} className="mb-6">
          <form className="grid gap-3 sm:grid-cols-3" onSubmit={submit}>
            <TextField label={t('admin.common.title')} value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            <SelectField label={t('admin.facilities.type')} value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as FacilityType })}>
              {types.map((x) => <option key={x} value={x}>{t(`admin.facilities.types.${x}`)}</option>)}
            </SelectField>
            <SelectField label={t('admin.common.faculty')} value={form.facultyId} onChange={(e) => setForm({ ...form, facultyId: e.target.value })}>
              <option value="">{t('admin.facilities.shared')}</option>
              {(faculties.data?.items ?? []).map((f) => <option key={f.id} value={f.id}>{localName(f, i18n.language)}</option>)}
            </SelectField>
            <TextField label={t('admin.facilities.capacity')} type="number" dir="ltr" value={form.capacity} onChange={(e) => setForm({ ...form, capacity: e.target.value })} required />
            <TextField label={t('admin.facilities.building')} value={form.building} onChange={(e) => setForm({ ...form, building: e.target.value })} />
            <TextField label={t('admin.facilities.floor')} value={form.floor} onChange={(e) => setForm({ ...form, floor: e.target.value })} />
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
        </Card>
      )}
      <QueryState query={facilities}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <Table head={[t('admin.common.title'), t('admin.facilities.type'), t('admin.common.faculty'), t('admin.facilities.capacity'), t('admin.facilities.building'), t('admin.common.status'), '']}>
              {data.items.map((f) => (
                <tr key={f.id}>
                  <td className="font-medium">{f.name}</td>
                  <td>{t(`admin.facilities.types.${f.type}`)}</td>
                  <td>{facultyName(f.facultyId)}</td>
                  <td className="ltr-nums">{f.capacity}</td>
                  <td>{[f.building, f.floor].filter(Boolean).join(' · ') || '—'}</td>
                  <td><Badge tone={f.isActive ? 'success' : 'neutral'}>{f.isActive ? t('admin.common.active') : t('admin.common.inactive')}</Badge></td>
                  <td><Button variant="ghost" className="min-h-8 px-2" onClick={() => open(f)}>{t('admin.common.edit')}</Button></td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
    </>
  );
}
