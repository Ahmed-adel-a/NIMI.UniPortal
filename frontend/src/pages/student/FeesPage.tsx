import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile } from '../../api/client';
import type { Schema } from '../../api/types';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card, Stat } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, EmptyState, QueryState } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { Table } from '../../components/ui/Table';
import { formatDate, formatDateTime, formatMoney } from '../../lib/format';

type FeeLine = Schema<'StudentFeeLineDto'>;

export function FeesPage() {
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [paying, setPaying] = useState<{ line: FeeLine; intentId: string } | null>(null);
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);

  const fees = useQuery({ queryKey: ['fees'], queryFn: () => call(api.GET('/api/payments/fees')) });
  const history = useQuery({ queryKey: ['payments', 'history'], queryFn: () => call(api.GET('/api/payments/history', { params: { query: { pageSize: 50 } } })) });

  const checkout = useMutation({
    mutationFn: (line: FeeLine) => call(api.POST('/api/payments/checkout', { body: { feeItemId: line.feeItemId, amount: line.balance } })),
    onSuccess: (result, line) => setPaying({ line, intentId: result.paymentIntentId }),
    onError: (error) => setMessage({ tone: 'danger', text: errorMessage(error) }),
  });

  // The mock gateway (Development) lets the payer pick the outcome; a real provider would redirect instead.
  const complete = useMutation({
    mutationFn: async (outcome: 'success' | 'fail') => {
      await call(api.POST('/api/payments/mock/{id}/complete', { params: { path: { id: paying!.intentId } }, body: { outcome } }));
      return call(api.GET('/api/payments/intents/{id}', { params: { path: { id: paying!.intentId } } }));
    },
    onSuccess: (intent) => {
      setPaying(null);
      setMessage(intent.receiptNumber ? { tone: 'success', text: t('fees.paidOk', { receipt: intent.receiptNumber }) } : { tone: 'danger', text: intent.failureReason ?? t('fees.paidFail') });
      void queryClient.invalidateQueries({ queryKey: ['fees'] });
      void queryClient.invalidateQueries({ queryKey: ['payments'] });
    },
    onError: (error) => setMessage({ tone: 'danger', text: errorMessage(error) }),
  });

  const money = (value: number | string) => `${formatMoney(value, i18n.language)} ${t('fees.currency')}`;

  return (
    <>
      <PageHeader title={t('fees.title')} />
      {message && <div className="mb-4"><Alert tone={message.tone}>{message.text}</Alert></div>}

      {paying && (
        <div className="mb-6">
          <Card title={t('fees.checkoutTitle')}>
            <p className="mb-3 text-sm text-muted">{t('fees.checkoutHint')}</p>
            <p className="mb-4 font-semibold">
              {ar ? paying.line.nameAr : paying.line.name} — {money(paying.line.balance)}
            </p>
            <div className="flex flex-wrap gap-2">
              <Button busy={complete.isPending} onClick={() => complete.mutate('success')}>{t('fees.succeed')}</Button>
              <Button variant="danger" busy={complete.isPending} onClick={() => complete.mutate('fail')}>{t('fees.fail')}</Button>
              <Button variant="ghost" onClick={() => setPaying(null)}>{t('app.cancel')}</Button>
            </div>
          </Card>
        </div>
      )}

      <QueryState query={fees}>
        {(data) => (
          <div className="grid gap-4">
            <div className="max-w-xs">
              <Stat label={t('fees.balance')} value={money(data.totalBalance)} tone={Number(data.totalBalance) > 0 ? 'warning' : 'success'} />
            </div>
            {data.fees.length === 0 ? (
              <EmptyState>{t('fees.empty')}</EmptyState>
            ) : (
              <Table head={[t('fees.item'), t('fees.due'), t('fees.paid'), t('fees.remaining'), t('fees.dueOn'), '']}>
                {data.fees.map((line) => (
                  <tr key={line.feeItemId}>
                    <td>
                      <span className="font-medium">{ar ? line.nameAr : line.name}</span>{' '}
                      <Badge tone={line.status === 'paid' ? 'success' : line.isOverdue ? 'danger' : 'warning'}>
                        {line.isOverdue && line.status !== 'paid' ? t('fees.overdue') : t(`fees.statuses.${line.status}`, { defaultValue: line.status })}
                      </Badge>
                    </td>
                    <td className="ltr-nums">{formatMoney(line.amountDue, i18n.language)}</td>
                    <td className="ltr-nums">{formatMoney(line.amountPaid, i18n.language)}</td>
                    <td className="font-semibold ltr-nums">{formatMoney(line.balance, i18n.language)}</td>
                    <td>{formatDate(line.dueOn, i18n.language)}</td>
                    <td>
                      {Number(line.balance) > 0 && (
                        <Button className="min-h-8" busy={checkout.isPending && checkout.variables?.feeItemId === line.feeItemId} onClick={() => checkout.mutate(line)}>
                          {t('fees.payOnline')}
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </Table>
            )}
          </div>
        )}
      </QueryState>

      <h2 className="mb-2 mt-8 text-lg font-semibold">{t('fees.history')}</h2>
      <QueryState query={history}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('fees.noHistory')}</EmptyState>
          ) : (
            <Table head={[t('fees.receipt'), t('fees.item'), t('fees.amount'), t('fees.method'), t('fees.date'), '']}>
              {data.items.map((p) => (
                <tr key={p.id}>
                  <td className="ltr-nums">{p.receiptNumber}</td>
                  <td>{ar ? p.feeNameAr : p.feeName}</td>
                  <td className="ltr-nums">{formatMoney(p.amount, i18n.language)}</td>
                  <td>{p.method}</td>
                  <td>{formatDateTime(p.paidAt, i18n.language)}</td>
                  <td>
                    <Button variant="ghost" className="min-h-8 px-2" onClick={() => void downloadFile(`/api/documents/receipts/${p.id}`, `${p.receiptNumber}.pdf`)}>
                      PDF
                    </Button>
                  </td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
    </>
  );
}
