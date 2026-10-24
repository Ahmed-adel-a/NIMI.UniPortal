import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile } from '../../api/client';
import { Button } from '../../components/ui/Button';
import { Stat } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { EmptyState, QueryState } from '../../components/ui/States';
import { Table } from '../../components/ui/Table';
import { formatNumber } from '../../lib/format';

export function GradesPage() {
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const grades = useQuery({ queryKey: ['grades'], queryFn: () => call(api.GET('/api/grades/mine')) });
  const termNames: Record<string, string> = { first: ar ? 'الأول' : '1', second: ar ? 'الثاني' : '2', summer: ar ? 'الصيفي' : 'Summer' };

  return (
    <>
      <PageHeader
        title={t('grades.title')}
        actions={
          <>
            <Button variant="secondary" onClick={() => void downloadFile('/api/documents/transcript', 'transcript.pdf')}>{t('grades.transcript')}</Button>
            <Button variant="secondary" onClick={() => void downloadFile('/api/documents/enrollment-certificate', 'enrollment-certificate.pdf')}>{t('grades.certificate')}</Button>
          </>
        }
      />
      <QueryState query={grades}>
        {(g) =>
          g.terms.length === 0 ? (
            <EmptyState>{t('grades.empty')}</EmptyState>
          ) : (
            <div className="grid gap-6">
              <div className="max-w-xs">
                <Stat label={t('grades.cumulative')} value={<span className="ltr-nums">{formatNumber(g.cumulativeGpa, i18n.language)}</span>} />
              </div>
              {g.terms.map((term) => (
                <section key={term.termId} className="grid gap-2">
                  <div className="flex flex-wrap items-baseline justify-between gap-2">
                    <h2 className="font-semibold">{t('grades.term', { number: termNames[term.number] ?? term.number, year: term.academicYear })}</h2>
                    <span className="text-sm text-muted">
                      {t('grades.termGpa')}: <span className="font-semibold text-fg ltr-nums">{formatNumber(term.termGpa, i18n.language)}</span>
                    </span>
                  </div>
                  <Table head={[t('grades.subject'), t('grades.credits'), t('grades.score'), t('grades.letter'), t('grades.points')]}>
                    {term.grades.map((grade) => (
                      <tr key={grade.gradeId}>
                        <td>
                          <span className="font-medium">{ar ? grade.subjectNameAr : grade.subjectName}</span>{' '}
                          <span className="text-xs text-muted ltr-nums">{grade.subjectCode}</span>
                        </td>
                        <td className="ltr-nums">{grade.creditHours}</td>
                        <td className="ltr-nums">{formatNumber(grade.score, i18n.language)}</td>
                        <td className="font-semibold ltr-nums">{grade.grade}</td>
                        <td className="ltr-nums">{formatNumber(grade.gradePoints, i18n.language)}</td>
                      </tr>
                    ))}
                  </Table>
                </section>
              ))}
            </div>
          )
        }
      </QueryState>
    </>
  );
}
