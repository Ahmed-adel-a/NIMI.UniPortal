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
import { formatDate, formatDateTime, formatMoney } from '../../lib/format';
import { useDepartments, useFaculties, useTerms } from './lookups';

const categories = ['tuition', 'military_education', 'activity', 'other'] as const;
const methods = ['cash', 'bank_transfer'] as const;
type Notify = (m: { tone: 'success' | 'danger'; text: string }) => void;

function useFeeItems() {
  return useQuery({ queryKey: ['admin', 'fee-items'], queryFn: () => call(api.GET('/api/admin/fee-items', { params: { query: { includeInactive: true, pageSize: 100 } } })) });
}

/** Fee items, office payments with receipts, the register and the expected-vs-collected report. */
export function PaymentsPage() {
  const { t } = useTranslation();
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  return (
    <>
      <PageHeader title={t('admin.payments.title')} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}
      <div className="grid gap-6">
        <RecordPayment onMessage={setMessage} />
        <FeeItems onMessage={setMessage} />
        <Report />
        <Register />
      </div>
    </>
  );
}

function RecordPayment({ onMessage }: { onMessage: Notify }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const feeItems = useFeeItems();
  const [search, setSearch] = useState('');
  const [student, setStudent] = useState<Schema<'UserDto'> | null>(null);
  const [form, setForm] = useState({ feeItemId: '', amount: '', method: 'cash', reference: '' });

  const students = useQuery({
    queryKey: ['admin', 'student-search', search],
    queryFn: () => call(api.GET('/api/admin/users', { params: { query: { role: 'student', search, pageSize: 8 } } })),
    enabled: search.trim().length >= 2 && !student,
  });

  const record = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/payments', {
        body: { studentId: student!.id, feeItemId: form.feeItemId, amount: Number(form.amount), method: form.method, reference: form.reference || null, paidAt: null },
      })),
    onSuccess: (r) => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'payments'] });
      setForm({ ...form, amount: '', reference: '' });
      onMessage({ tone: 'success', text: `${t('admin.payments.recorded', { receipt: r.receiptNumber })} · ${t('admin.payments.balance')}: ${formatMoney(r.balance, i18n.language)}` });
    },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    record.mutate();
  }

  return (
    <Card title={t('admin.payments.record')}>
      {student ? (
        <div className="mb-3 flex items-center gap-3 text-sm">
          <span className="font-semibold">{student.fullName}</span>
          <span className="ltr-nums text-muted">{student.studentCode}</span>
          <Button variant="ghost" className="min-h-8 px-2" onClick={() => { setStudent(null); setSearch(''); }}>{t('admin.common.change')}</Button>
        </div>
      ) : (
        <div className="mb-3 max-w-md">
          <TextField label={t('admin.payments.student')} placeholder={t('admin.payments.studentSearch')} value={search} onChange={(e) => setSearch(e.target.value)} />
          {(students.data?.items ?? []).length > 0 && (
            <ul className="mt-1 divide-y divide-line rounded-lg border border-line">
              {students.data!.items.map((s) => (
                <li key={s.id}>
                  <button type="button" className="w-full px-3 py-2 text-start text-sm hover:bg-sunk" onClick={() => setStudent(s)}>
                    {s.fullName} <span className="ltr-nums text-muted">{s.studentCode}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
      {student && (
        <form className="grid gap-3 sm:grid-cols-2 lg:grid-cols-5" onSubmit={submit}>
          <SelectField label={t('admin.payments.feeItem')} value={form.feeItemId} onChange={(e) => {
            const item = feeItems.data?.items.find((x) => x.id === e.target.value);
            setForm({ ...form, feeItemId: e.target.value, amount: item ? String(item.amount) : form.amount });
          }} required>
            <option value="">{t('admin.common.select')}</option>
            {(feeItems.data?.items ?? []).filter((x) => x.isActive).map((x) => <option key={x.id} value={x.id}>{localName(x, i18n.language)}</option>)}
          </SelectField>
          <TextField label={t('admin.payments.amount')} type="number" step="0.01" min="0" dir="ltr" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} required />
          <SelectField label={t('admin.payments.method')} value={form.method} onChange={(e) => setForm({ ...form, method: e.target.value })}>
            {methods.map((m) => <option key={m} value={m}>{t(`admin.payments.methods.${m}`)}</option>)}
          </SelectField>
          <TextField label={t('admin.payments.reference')} dir="ltr" value={form.reference} onChange={(e) => setForm({ ...form, reference: e.target.value })} />
          <div className="flex items-end"><Button type="submit" busy={record.isPending}>{t('admin.payments.recordButton')}</Button></div>
        </form>
      )}
    </Card>
  );
}

function FeeItems({ onMessage }: { onMessage: Notify }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const feeItems = useFeeItems();
  const terms = useTerms();
  const faculties = useFaculties();
  const departments = useDepartments();
  const [creating, setCreating] = useState(false);
  const [form, setForm] = useState({ name: '', nameAr: '', description: '', category: 'tuition', amount: '', termId: '', facultyId: '', departmentId: '', dueOn: '' });

  const create = useMutation({
    mutationFn: () =>
      call(api.POST('/api/admin/fee-items', {
        body: {
          name: form.name, nameAr: form.nameAr, description: form.description || null, category: form.category, amount: Number(form.amount),
          termId: form.termId || null, facultyId: form.facultyId || null, departmentId: form.departmentId || null, dueOn: form.dueOn,
        },
      })),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['admin', 'fee-items'] });
      setCreating(false);
      onMessage({ tone: 'success', text: t('admin.common.saved') });
    },
    onError: (e) => onMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  const scopeOf = (x: Schema<'FeeItemDto'>) => {
    if (x.departmentId) return localName(departments.data?.items.find((d) => d.id === x.departmentId), i18n.language);
    if (x.facultyId) return localName(faculties.data?.items.find((f) => f.id === x.facultyId), i18n.language);
    return t('admin.dashboard.university');
  };

  return (
    <Card title={t('admin.payments.items')} actions={<Button variant="secondary" onClick={() => setCreating(!creating)}>{t('admin.payments.newItem')}</Button>}>
      {creating && (
        <form className="mb-4 grid gap-3 sm:grid-cols-3" onSubmit={submit}>
          <TextField label={t('admin.common.nameAr')} value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} required />
          <TextField label={t('admin.common.name')} dir="ltr" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          <SelectField label={t('admin.payments.category')} value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })}>
            {categories.map((c) => <option key={c} value={c}>{t(`admin.payments.categories.${c}`)}</option>)}
          </SelectField>
          <TextField label={t('admin.payments.amount')} type="number" step="0.01" min="0" dir="ltr" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} required />
          <TextField label={t('admin.payments.dueOn')} type="date" value={form.dueOn} onChange={(e) => setForm({ ...form, dueOn: e.target.value })} required />
          <SelectField label={t('admin.common.term')} value={form.termId} onChange={(e) => setForm({ ...form, termId: e.target.value })}>
            <option value="">{t('admin.common.all')}</option>
            {(terms.data?.items ?? []).map((x) => <option key={x.id} value={x.id}>{t(`admin.terms.numbers.${x.number}`)} {x.academicYear}</option>)}
          </SelectField>
          <SelectField label={t('admin.common.faculty')} value={form.facultyId} onChange={(e) => setForm({ ...form, facultyId: e.target.value, departmentId: '' })}>
            <option value="">{t('admin.common.all')}</option>
            {(faculties.data?.items ?? []).map((f) => <option key={f.id} value={f.id}>{localName(f, i18n.language)}</option>)}
          </SelectField>
          <SelectField label={t('admin.common.department')} value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value })}>
            <option value="">{t('admin.common.all')}</option>
            {(departments.data?.items ?? []).filter((d) => !form.facultyId || d.facultyId === form.facultyId).map((d) => <option key={d.id} value={d.id}>{d.facultyCode}/{d.code}</option>)}
          </SelectField>
          <TextField label={t('admin.common.description')} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          <div className="sm:col-span-3"><Button type="submit" busy={create.isPending}>{t('admin.common.create')}</Button></div>
        </form>
      )}
      <QueryState query={feeItems}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <Table head={[t('admin.common.title'), t('admin.payments.category'), t('admin.payments.amount'), t('admin.payments.dueOn'), t('admin.payments.scope'), t('admin.common.status')]}>
              {data.items.map((x) => (
                <tr key={x.id}>
                  <td className="font-medium">{localName(x, i18n.language)}</td>
                  <td>{t(`admin.payments.categories.${x.category}`, { defaultValue: x.category })}</td>
                  <td className="ltr-nums">{formatMoney(x.amount, i18n.language)}</td>
                  <td>{formatDate(x.dueOn, i18n.language)}</td>
                  <td>{scopeOf(x)}</td>
                  <td><Badge tone={x.isActive ? 'success' : 'neutral'}>{x.isActive ? t('admin.common.active') : t('admin.common.inactive')}</Badge></td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
    </Card>
  );
}

function Report() {
  const { t, i18n } = useTranslation();
  const report = useQuery({ queryKey: ['admin', 'payments', 'report'], queryFn: () => call(api.GET('/api/admin/payments/report')) });
  return (
    <Card title={t('admin.payments.report')}>
      <QueryState query={report}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <Table head={[t('admin.common.title'), t('admin.payments.payers'), t('admin.payments.expected'), t('admin.payments.collected'), t('admin.payments.outstanding')]}>
              {data.items.map((r) => (
                <tr key={r.feeItemId}>
                  <td>{localName(r, i18n.language)}</td>
                  <td className="ltr-nums">{r.payers}</td>
                  <td className="ltr-nums">{formatMoney(r.expected, i18n.language)}</td>
                  <td className="ltr-nums">{formatMoney(r.collected, i18n.language)}</td>
                  <td className="ltr-nums">{formatMoney(r.outstanding, i18n.language)}</td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
    </Card>
  );
}

function Register() {
  const { t, i18n } = useTranslation();
  const feeItems = useFeeItems();
  const [page, setPage] = useState(1);
  const payments = useQuery({ queryKey: ['admin', 'payments', 'register', page], queryFn: () => call(api.GET('/api/admin/payments', { params: { query: { page, pageSize: 25 } } })) });
  const feeName = (id: string) => localName(feeItems.data?.items.find((x) => x.id === id), i18n.language) || '—';
  return (
    <Card title={t('admin.payments.register')}>
      <QueryState query={payments}>
        {(data) =>
          data.items.length === 0 ? <EmptyState>{t('app.none')}</EmptyState> : (
            <>
              <Table head={[t('admin.payments.receipt'), t('admin.payments.feeItem'), t('admin.payments.amount'), t('admin.payments.method'), t('admin.payments.paidAt')]}>
                {data.items.map((p) => (
                  <tr key={p.id}>
                    <td className="ltr-nums">{p.receiptNumber}</td>
                    <td>{feeName(p.feeItemId)}</td>
                    <td className="ltr-nums">{formatMoney(p.amount, i18n.language)}</td>
                    <td>{t(`admin.payments.methods.${p.method}`, { defaultValue: p.method })}</td>
                    <td>{formatDateTime(p.paidAt, i18n.language)}</td>
                  </tr>
                ))}
              </Table>
              <Pager page={page} totalPages={data.totalPages ?? 1} onPage={setPage} />
            </>
          )
        }
      </QueryState>
    </Card>
  );
}
