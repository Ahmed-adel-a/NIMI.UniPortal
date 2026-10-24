import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import type { Schema } from '../../api/types';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { SelectField, TextField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, QueryState } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { jsDayOf } from '../../lib/format';

/** Settings use the API's weekday numbers: 0 = Saturday … 6 = Friday. */
const productDays = [0, 1, 2, 3, 4, 5, 6];

/** University-wide defaults; faculties may override some of them through the API. */
export function SettingsPage() {
  const { t } = useTranslation();
  const settings = useQuery({ queryKey: ['admin', 'settings'], queryFn: () => call(api.GET('/api/admin/settings')) });
  const [saved, setSaved] = useState(false);
  return (
    <>
      <PageHeader title={t('admin.settings.title')} />
      {saved && <div className="mb-4"><Alert tone="success">{t('admin.common.saved')}</Alert></div>}
      <QueryState query={settings}>{(data) => <SettingsForm key={data.version} settings={data} onSaved={setSaved} />}</QueryState>
    </>
  );
}

function SettingsForm({ settings, onSaved }: { settings: Schema<'AdminSettingsDto'>; onSaved: (saved: boolean) => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const days: string[] = t('days', { returnObjects: true }) as unknown as string[];
  const [form, setForm] = useState({
    universityName: settings.universityName,
    universityNameAr: settings.universityNameAr,
    timeZone: settings.timeZone,
    weekStartDay: settings.weekStartDay,
    workingDays: settings.workingDays,
    attendanceThreshold: String(settings.attendanceThreshold),
    lateGraceMinutes: String(settings.lateGraceMinutes),
    studentCodeFormat: settings.studentCodeFormat,
    militaryMode: settings.militaryEducationPolicy.mode,
  });
  const [scale, setScale] = useState(settings.gradingScale.map((b) => ({ grade: b.grade, minScore: String(b.minScore), points: String(b.points) })));
  const save = useMutation({
    mutationFn: () =>
      call(api.PUT('/api/admin/settings', {
        body: {
          universityName: form.universityName,
          universityNameAr: form.universityNameAr,
          domain: settings.domain,
          logoUrl: settings.logoUrl,
          primaryColor: settings.primaryColor,
          secondaryColor: settings.secondaryColor,
          timeZone: form.timeZone,
          weekStartDay: form.weekStartDay,
          workingDays: [...form.workingDays].sort((a, b) => a - b),
          attendanceThreshold: Number(form.attendanceThreshold),
          lateGraceMinutes: Number(form.lateGraceMinutes),
          gradingScale: scale.map((b) => ({ grade: b.grade, minScore: Number(b.minScore), points: Number(b.points) })),
          studentCodeFormat: form.studentCodeFormat,
          militaryEducationPolicy: { mode: form.militaryMode },
          version: settings.version,
        },
      })),
    onSuccess: () => {
      onSaved(true);
      void queryClient.invalidateQueries({ queryKey: ['admin', 'settings'] });
    },
    onError: () => onSaved(false),
  });

  function toggleDay(day: number) {
    setForm({ ...form, workingDays: form.workingDays.includes(day) ? form.workingDays.filter((d) => d !== day) : [...form.workingDays, day] });
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    onSaved(false);
    save.mutate();
  }

  return (
    <form className="grid gap-6" onSubmit={submit}>
      <Card title={t('admin.settings.general')}>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          <TextField label={t('admin.settings.universityNameAr')} value={form.universityNameAr} onChange={(e) => setForm({ ...form, universityNameAr: e.target.value })} required />
          <TextField label={t('admin.settings.universityName')} dir="ltr" value={form.universityName} onChange={(e) => setForm({ ...form, universityName: e.target.value })} required />
          <TextField label={t('admin.settings.timeZone')} dir="ltr" value={form.timeZone} onChange={(e) => setForm({ ...form, timeZone: e.target.value })} required />
          <SelectField label={t('admin.settings.weekStart')} value={form.weekStartDay} onChange={(e) => setForm({ ...form, weekStartDay: Number(e.target.value) })}>
            {productDays.map((d) => <option key={d} value={d}>{days[jsDayOf(d)]}</option>)}
          </SelectField>
          <TextField label={t('admin.settings.codeFormat')} dir="ltr" value={form.studentCodeFormat} onChange={(e) => setForm({ ...form, studentCodeFormat: e.target.value })} required />
        </div>
        <fieldset className="mt-4">
          <legend className="mb-2 text-sm font-medium">{t('admin.settings.workingDays')}</legend>
          <div className="flex flex-wrap gap-3">
            {productDays.map((d) => (
              <label key={d} className="flex items-center gap-1.5 text-sm">
                <input type="checkbox" checked={form.workingDays.includes(d)} onChange={() => toggleDay(d)} />
                {days[jsDayOf(d)]}
              </label>
            ))}
          </div>
        </fieldset>
      </Card>

      <Card title={t('admin.settings.academic')}>
        <div className="grid gap-3 sm:grid-cols-3">
          <TextField label={t('admin.settings.threshold')} type="number" min="0" max="100" step="0.5" dir="ltr" value={form.attendanceThreshold} onChange={(e) => setForm({ ...form, attendanceThreshold: e.target.value })} required />
          <TextField label={t('admin.settings.grace')} type="number" min="0" dir="ltr" value={form.lateGraceMinutes} onChange={(e) => setForm({ ...form, lateGraceMinutes: e.target.value })} required />
          <SelectField label={t('admin.settings.military')} value={form.militaryMode} onChange={(e) => setForm({ ...form, militaryMode: e.target.value as typeof form.militaryMode })}>
            <option value="opt_in">{t('admin.settings.modes.opt_in')}</option>
            <option value="mandatory">{t('admin.settings.modes.mandatory')}</option>
          </SelectField>
        </div>
      </Card>

      <Card title={t('admin.settings.scale')} actions={<Button type="button" variant="secondary" onClick={() => setScale([...scale, { grade: '', minScore: '0', points: '0' }])}>{t('admin.settings.addBand')}</Button>}>
        <div className="grid gap-2">
          {scale.map((band, i) => (
            <div key={i} className="grid grid-cols-[1fr_1fr_1fr_auto] items-end gap-2">
              <TextField label={t('admin.settings.grade')} dir="ltr" value={band.grade} onChange={(e) => setScale(scale.map((b, j) => (j === i ? { ...b, grade: e.target.value } : b)))} required />
              <TextField label={t('admin.settings.minScore')} type="number" step="0.01" dir="ltr" value={band.minScore} onChange={(e) => setScale(scale.map((b, j) => (j === i ? { ...b, minScore: e.target.value } : b)))} required />
              <TextField label={t('admin.settings.points')} type="number" step="0.01" dir="ltr" value={band.points} onChange={(e) => setScale(scale.map((b, j) => (j === i ? { ...b, points: e.target.value } : b)))} required />
              <Button type="button" variant="ghost" aria-label={t('admin.settings.removeBand')} onClick={() => setScale(scale.filter((_, j) => j !== i))}>✕</Button>
            </div>
          ))}
        </div>
      </Card>

      {save.isError && <Alert>{errorMessage(save.error)}</Alert>}
      <div><Button type="submit" busy={save.isPending}>{t('admin.common.save')}</Button></div>
    </form>
  );
}
