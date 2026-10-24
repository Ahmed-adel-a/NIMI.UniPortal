import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Stat } from '../../components/ui/Card';
import { SelectField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, QueryState } from '../../components/ui/States';
import { localName } from '../../i18n';
import { formatNumber } from '../../lib/format';
import { useFaculties } from './lookups';

/** Student Affairs metrics for the active term, for the whole university or one faculty. */
export function AdminDashboardPage() {
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const [facultyId, setFacultyId] = useState('');
  const faculties = useFaculties();

  const metrics = useQuery({ queryKey: ['admin', 'metrics'], queryFn: () => call(api.GET('/api/admin/dashboard/metrics')) });
  const summary = useQuery({
    queryKey: ['admin', 'summary', facultyId],
    queryFn: () => call(api.GET('/api/admin/dashboard/summary', { params: { query: { facultyId: facultyId || undefined } } })),
  });

  return (
    <>
      <PageHeader
        title={t('admin.dashboard.title')}
        actions={
          <div className="w-64">
            <SelectField label={t('admin.dashboard.scope')} value={facultyId} onChange={(e) => setFacultyId(e.target.value)}>
              <option value="">{t('admin.dashboard.university')}</option>
              {(faculties.data?.items ?? []).map((f) => <option key={f.id} value={f.id}>{localName(f, i18n.language)}</option>)}
            </SelectField>
          </div>
        }
      />
      <QueryState query={metrics}>
        {(defs) => (
          <QueryState query={summary}>
            {(s) => (
              <div className="grid gap-4">
                {s.unavailable.length > 0 && <Alert tone="warning">{t('errors.unavailable')}</Alert>}
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4">
                  {defs.map((m) => {
                    const value = s.values[m.key];
                    const shown = value === null || value === undefined ? '—' : `${formatNumber(value, i18n.language)}${m.unit === 'percent' ? '%' : ''}`;
                    return <Stat key={m.key} label={ar ? m.titleAr : m.title} value={<span className="ltr-nums">{shown}</span>} />;
                  })}
                </div>
              </div>
            )}
          </QueryState>
        )}
      </QueryState>
    </>
  );
}
