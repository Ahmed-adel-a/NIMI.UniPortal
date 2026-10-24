import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile } from '../../../api/client';
import { Button } from '../../../components/ui/Button';
import { EmptyState, QueryState } from '../../../components/ui/States';
import { Table } from '../../../components/ui/Table';
import { formatNumber } from '../../../lib/format';
import type { TeachingClass } from '../ClassPage';

/** The group's students with their attendance so far. */
export function RosterTab({ current }: { current: TeachingClass }) {
  const { t, i18n } = useTranslation();
  const summary = useQuery({
    queryKey: ['attendance', 'summary', current.groupId],
    queryFn: () => call(api.GET('/api/attendance/groups/{groupId}/summary', { params: { path: { groupId: current.groupId }, query: { pageSize: 100 } } })),
  });

  return (
    <div className="grid gap-3">
      <div>
        <Button variant="secondary" onClick={() => void downloadFile(`/api/attendance/groups/${current.groupId}/export`, `attendance-${current.subjectCode}-G${current.groupNumber}.csv`)}>
          {t('teach.roster.export')}
        </Button>
      </div>
      <QueryState query={summary}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('teach.roster.empty')}</EmptyState>
          ) : (
            <Table head={[t('teach.roster.student'), t('teach.roster.code'), t('teach.roster.attended'), t('teach.roster.held'), t('teach.roster.rate')]}>
              {data.items.map((s) => (
                <tr key={s.studentId}>
                  <td className="font-medium">{s.fullName}</td>
                  <td className="ltr-nums">{s.studentCode}</td>
                  <td className="ltr-nums">{s.attended}</td>
                  <td className="ltr-nums">{s.held}</td>
                  <td className="ltr-nums">{s.held > 0 ? `${formatNumber(s.percentage, i18n.language, 0)}%` : '—'}</td>
                </tr>
              ))}
            </Table>
          )
        }
      </QueryState>
    </div>
  );
}
