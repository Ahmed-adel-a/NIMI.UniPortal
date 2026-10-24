import { useState, type FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import type { Schema } from '../../api/types';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { SelectField, TextField } from '../../components/ui/Field';
import { Alert } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { plusWeeks } from '../../lib/format';

/**
 * Creates a draft term from an earlier one. Starts from the source term's number and week count, the next academic
 * year, and its dates moved 52 weeks forward (the same weekdays), all editable.
 */
export function CopyTermForm({
  source,
  onDone,
  onCancel,
}: {
  source: Schema<'TermDto'>;
  onDone: (result: Schema<'CopyTermResultDto'>) => void;
  onCancel: () => void;
}) {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({
    academicYear: String(source.academicYear + 1),
    number: source.number as string,
    startsOn: plusWeeks(source.startsOn, 52),
    endsOn: plusWeeks(source.endsOn, 52),
    weekCount: String(source.weekCount),
  });

  const copy = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/terms/{id}/copy', {
        params: { path: { id: source.id } },
        body: {
          academicYear: Number(form.academicYear),
          number: form.number as 'first',
          startsOn: form.startsOn,
          endsOn: form.endsOn,
          weekCount: Number(form.weekCount),
        },
      })),
    onSuccess: onDone,
    onError: (e) => setError(errorMessage(e)),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    copy.mutate();
  }

  return (
    <Card
      title={t('admin.terms.copyTitle', { term: `${t(`admin.terms.numbers.${source.number}`)} ${source.academicYear}` })}
      className="mb-6"
    >
      <p className="mb-4 text-sm text-muted">{t('admin.terms.copyHint')}</p>
      {error && <div className="mb-4"><Alert>{error}</Alert></div>}
      <form className="grid gap-3 sm:grid-cols-3 lg:grid-cols-5" onSubmit={submit}>
        <TextField label={t('admin.terms.year')} type="number" dir="ltr" value={form.academicYear} onChange={(e) => setForm({ ...form, academicYear: e.target.value })} required />
        <SelectField label={t('admin.terms.number')} value={form.number} onChange={(e) => setForm({ ...form, number: e.target.value })}>
          {(['first', 'second', 'summer'] as const).map((n) => <option key={n} value={n}>{t(`admin.terms.numbers.${n}`)}</option>)}
        </SelectField>
        <TextField label={t('admin.terms.starts')} type="date" value={form.startsOn} onChange={(e) => setForm({ ...form, startsOn: e.target.value })} required />
        <TextField label={t('admin.terms.ends')} type="date" value={form.endsOn} onChange={(e) => setForm({ ...form, endsOn: e.target.value })} required />
        <TextField label={t('admin.terms.weeks')} type="number" dir="ltr" value={form.weekCount} onChange={(e) => setForm({ ...form, weekCount: e.target.value })} required />
        <div className="flex gap-2 sm:col-span-3 lg:col-span-5">
          <Button type="submit" busy={copy.isPending}>{t('admin.terms.copyCreate')}</Button>
          <Button variant="secondary" onClick={onCancel}>{t('app.cancel')}</Button>
        </div>
      </form>
    </Card>
  );
}
