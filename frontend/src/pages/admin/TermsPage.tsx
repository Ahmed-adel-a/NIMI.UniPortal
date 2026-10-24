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
import { formatDate, formatDateTime } from '../../lib/format';
import { CopyTermForm } from './CopyTermForm';
import { CopyTermResult } from './CopyTermResult';
import { useTerms } from './lookups';

const statusTone = { planned: 'neutral', active: 'success', closed: 'warning' } as const;

/** Terms (one active at a time) and the credit-hours registration windows. */
export function TermsPage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const terms = useTerms();
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const [creating, setCreating] = useState(false);
  const [copying, setCopying] = useState<Schema<'TermDto'> | null>(null);
  const [copyResult, setCopyResult] = useState<Schema<'CopyTermResultDto'> | null>(null);
  const year = new Date().getFullYear();
  const [form, setForm] = useState({ academicYear: String(year), number: 'first', startsOn: '', endsOn: '', weekCount: '15' });

  const refresh = () => {
    void queryClient.invalidateQueries({ queryKey: ['lookup', 'terms'] });
    void queryClient.invalidateQueries({ queryKey: ['lookup', 'current-term'] });
  };
  const onError = (e: unknown) => setMessage({ tone: 'danger', text: errorMessage(e) });

  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/terms', {
        body: { academicYear: Number(form.academicYear), number: form.number as 'first', startsOn: form.startsOn, endsOn: form.endsOn, weekCount: Number(form.weekCount) },
      })),
    onSuccess: () => { refresh(); setCreating(false); setMessage({ tone: 'success', text: t('admin.common.saved') }); },
    onError,
  });
  const transition = useMutation({
    mutationFn: (v: { id: string; to: 'activate' | 'close' }) =>
      v.to === 'activate'
        ? call(api.POST('/api/admin/terms/{id}/activate', { params: { path: { id: v.id } } }))
        : call(api.POST('/api/admin/terms/{id}/close', { params: { path: { id: v.id } } })),
    onSuccess: () => { refresh(); setMessage({ tone: 'success', text: t('admin.common.saved') }); },
    onError,
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  const termName = (id: string) => {
    const term = terms.data?.items.find((x) => x.id === id);
    return term ? `${t(`admin.terms.numbers.${term.number}`)} ${term.academicYear}` : '—';
  };

  return (
    <>
      <PageHeader title={t('admin.terms.title')} actions={<Button onClick={() => setCreating(!creating)}>{t('admin.terms.new')}</Button>} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      {copying && (
        <CopyTermForm
          key={copying.id}
          source={copying}
          onCancel={() => setCopying(null)}
          onDone={(result) => { refresh(); setCopying(null); setCopyResult(result); }}
        />
      )}
      {copyResult && <CopyTermResult result={copyResult} onClose={() => setCopyResult(null)} />}
      {creating && (
        <Card title={t('admin.terms.new')} className="mb-6">
          <form className="grid gap-3 sm:grid-cols-3 lg:grid-cols-5" onSubmit={submit}>
            <TextField label={t('admin.terms.year')} type="number" dir="ltr" value={form.academicYear} onChange={(e) => setForm({ ...form, academicYear: e.target.value })} required />
            <SelectField label={t('admin.terms.number')} value={form.number} onChange={(e) => setForm({ ...form, number: e.target.value })}>
              {(['first', 'second', 'summer'] as const).map((n) => <option key={n} value={n}>{t(`admin.terms.numbers.${n}`)}</option>)}
            </SelectField>
            <TextField label={t('admin.terms.starts')} type="date" value={form.startsOn} onChange={(e) => setForm({ ...form, startsOn: e.target.value })} required />
            <TextField label={t('admin.terms.ends')} type="date" value={form.endsOn} onChange={(e) => setForm({ ...form, endsOn: e.target.value })} required />
            <TextField label={t('admin.terms.weeks')} type="number" dir="ltr" value={form.weekCount} onChange={(e) => setForm({ ...form, weekCount: e.target.value })} required />
            <div className="sm:col-span-3 lg:col-span-5"><Button type="submit" busy={create.isPending}>{t('admin.common.create')}</Button></div>
          </form>
        </Card>
      )}
      <div className="grid gap-6">
        <Card title={t('admin.terms.title')}>
          <QueryState query={terms}>
            {(data) =>
              data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
                <Table head={[t('admin.terms.number'), t('admin.terms.starts'), t('admin.terms.ends'), t('admin.terms.weeks'), t('admin.common.status'), '']}>
                  {data.items.map((term) => (
                    <tr key={term.id}>
                      <td>{t(`admin.terms.numbers.${term.number}`)} <span className="ltr-nums">{term.academicYear}</span></td>
                      <td>{formatDate(term.startsOn, i18n.language)}</td>
                      <td>{formatDate(term.endsOn, i18n.language)}</td>
                      <td className="ltr-nums">{term.weekCount}</td>
                      <td><Badge tone={statusTone[term.status]}>{t(`admin.terms.statuses.${term.status}`)}</Badge></td>
                      <td>
                        {term.status === 'planned' && <Button variant="ghost" className="min-h-8 px-2" onClick={() => transition.mutate({ id: term.id, to: 'activate' })}>{t('admin.terms.activate')}</Button>}
                        {term.status === 'active' && <Button variant="ghost" className="min-h-8 px-2" onClick={() => window.confirm(t('admin.terms.confirmClose')) && transition.mutate({ id: term.id, to: 'close' })}>{t('admin.terms.close')}</Button>}
                        <Button variant="ghost" className="min-h-8 px-2" onClick={() => { setCopyResult(null); setCopying(term); }}>{t('admin.terms.copy')}</Button>
                      </td>
                    </tr>
                  ))}
                </Table>
              )
            }
          </QueryState>
        </Card>
        <RegistrationWindows termName={termName} onMessage={setMessage} />
      </div>
    </>
  );
}

function RegistrationWindows({ termName, onMessage }: { termName: (id: string) => string; onMessage: (m: { tone: 'success' | 'danger'; text: string }) => void }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const terms = useTerms();
  const windows = useQuery({ queryKey: ['admin', 'registration-windows'], queryFn: () => call(api.GET('/api/admin/registration-windows', { params: { query: { pageSize: 100 } } })) });
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState({ termId: '', opensAt: '', closesAt: '', minCredits: '12', maxCredits: '18' });

  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/registration-windows', {
        body: { termId: form.termId, opensAt: new Date(form.opensAt).toISOString(), closesAt: new Date(form.closesAt).toISOString(), minCredits: Number(form.minCredits), maxCredits: Number(form.maxCredits) },
      })),
    onSuccess: () => { void queryClient.invalidateQueries({ queryKey: ['admin', 'registration-windows'] }); setCreating(false); onMessage({ tone: 'success', text: t('admin.common.saved') }); },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <Card title={t('admin.terms.windows')} actions={<Button variant="secondary" onClick={() => setCreating(!creating)}>{t('admin.terms.newWindow')}</Button>}>
      {creating && (
        <form className="mb-4 grid gap-3 sm:grid-cols-3 lg:grid-cols-5" onSubmit={submit}>
          <SelectField label={t('admin.common.term')} value={form.termId} onChange={(e) => setForm({ ...form, termId: e.target.value })} required>
            <option value="">{t('admin.common.select')}</option>
            {(terms.data?.items ?? []).filter((x) => x.status !== 'closed').map((x) => <option key={x.id} value={x.id}>{termName(x.id)}</option>)}
          </SelectField>
          <TextField label={t('admin.terms.opens')} type="datetime-local" value={form.opensAt} onChange={(e) => setForm({ ...form, opensAt: e.target.value })} required />
          <TextField label={t('admin.terms.closes')} type="datetime-local" value={form.closesAt} onChange={(e) => setForm({ ...form, closesAt: e.target.value })} required />
          <TextField label={t('admin.terms.minCredits')} type="number" dir="ltr" value={form.minCredits} onChange={(e) => setForm({ ...form, minCredits: e.target.value })} required />
          <TextField label={t('admin.terms.maxCredits')} type="number" dir="ltr" value={form.maxCredits} onChange={(e) => setForm({ ...form, maxCredits: e.target.value })} required />
          <div className="sm:col-span-3 lg:col-span-5"><Button type="submit" busy={create.isPending}>{t('admin.common.create')}</Button></div>
        </form>
      )}
      <QueryState query={windows}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <Table head={[t('admin.common.term'), t('admin.terms.opens'), t('admin.terms.closes'), t('admin.terms.minCredits'), t('admin.terms.maxCredits'), t('admin.common.status')]}>
              {data.items.map((w) => (
                <tr key={w.id}>
                  <td>{termName(w.termId)}</td>
                  <td>{formatDateTime(w.opensAt, i18n.language)}</td>
                  <td>{formatDateTime(w.closesAt, i18n.language)}</td>
                  <td className="ltr-nums">{w.minCredits}</td>
                  <td className="ltr-nums">{w.maxCredits}</td>
                  <td>{w.isOpen ? <Badge tone="success">{t('admin.terms.open')}</Badge> : <Badge>{t('admin.terms.statuses.closed')}</Badge>}</td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
    </Card>
  );
}
