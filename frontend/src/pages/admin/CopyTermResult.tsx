import { useTranslation } from 'react-i18next';
import type { Schema } from '../../api/types';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { Alert } from '../../components/ui/States';
import { Table } from '../../components/ui/Table';

/** What a term copy carried over, and what it could not (each with the reason, in the app's language). */
export function CopyTermResult({ result, onClose }: { result: Schema<'CopyTermResultDto'>; onClose: () => void }) {
  const { t } = useTranslation();
  const name = `${t(`admin.terms.numbers.${result.term.number}`)} ${result.term.academicYear}`;

  return (
    <Card title={t('admin.terms.copyDone', { term: name })} actions={<Button variant="ghost" onClick={onClose}>{t('app.close')}</Button>} className="mb-6">
      <div className="grid gap-4">
        <Alert tone="success">
          {t('admin.terms.copied', {
            groups: result.copied.groups,
            assignments: result.copied.teachingAssignments,
            slots: result.copied.slots,
          })}
        </Alert>
        <p className="text-sm text-muted">{t('admin.terms.reviewHint')}</p>
        {result.skipped.length === 0 ? (
          <p className="text-sm">{t('admin.terms.skippedNone')}</p>
        ) : (
          <div>
            <h3 className="mb-2 font-semibold">{t('admin.terms.skippedTitle', { n: result.skipped.length })}</h3>
            <Table head={[t('admin.terms.skippedWhat'), t('admin.terms.skippedSubject'), t('admin.terms.skippedWhy')]}>
              {result.skipped.map((item, index) => (
                <tr key={index}>
                  <td>{t(`admin.terms.kinds.${item.kind}`)}</td>
                  <td>
                    <span className="ltr-nums">{item.subjectCode}</span>
                    {item.groupNumber !== null && <> · {t('dashboard.group')} <span className="ltr-nums">{item.groupNumber}</span></>}
                  </td>
                  <td>{t(`admin.terms.reasons.${item.reason}`)}</td>
                </tr>
              ))}
            </Table>
          </div>
        )}
      </div>
    </Card>
  );
}
