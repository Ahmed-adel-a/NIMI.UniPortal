import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { Schema } from '../../api/types';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert, QueryState } from '../../components/ui/States';
import { AnnouncementsTab } from './class/AnnouncementsTab';
import { AssignmentsTab } from './class/AssignmentsTab';
import { AttendanceTab } from './class/AttendanceTab';
import { GradesTab } from './class/GradesTab';
import { MaterialsTab } from './class/MaterialsTab';
import { RosterTab } from './class/RosterTab';
import { useMyClasses } from './useTeaching';

export type TeachingClass = Schema<'TeachingClassDto'>;
const tabs = ['roster', 'attendance', 'grades', 'materials', 'announcements', 'assignments'] as const;
type Tab = (typeof tabs)[number];

/** One group the caller teaches: everything the teacher does for it, in tabs. */
export function ClassPage() {
  const { groupId } = useParams();
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const classes = useMyClasses();
  const [tab, setTab] = useState<Tab>('roster');

  return (
    <QueryState query={classes}>
      {(list) => {
        const current = list.find((c) => c.groupId === groupId);
        if (!current) return <Alert>{t('errors.notFound')}</Alert>;
        return (
          <>
            <Link to="/teach" className="text-sm text-primary hover:underline">← {t('teach.back')}</Link>
            <PageHeader
              title={ar ? current.subjectNameAr : current.subjectName}
              subtitle={<><span className="ltr-nums">{current.subjectCode}</span> · {t('teach.group', { n: current.groupNumber })}</>}
            />
            <div role="tablist" className="mb-5 flex flex-wrap gap-1 border-b border-line">
              {tabs.map((name) => (
                <button
                  key={name}
                  role="tab"
                  aria-selected={tab === name}
                  onClick={() => setTab(name)}
                  className={`-mb-px border-b-2 px-3 py-2 text-sm font-medium ${tab === name ? 'border-primary text-primary' : 'border-transparent text-muted hover:text-fg'}`}
                >
                  {t(`teach.tabs.${name}`)}
                </button>
              ))}
            </div>
            {tab === 'roster' && <RosterTab current={current} />}
            {tab === 'attendance' && <AttendanceTab current={current} />}
            {tab === 'grades' && <GradesTab current={current} />}
            {tab === 'materials' && <MaterialsTab current={current} />}
            {tab === 'announcements' && <AnnouncementsTab current={current} />}
            {tab === 'assignments' && <AssignmentsTab current={current} />}
          </>
        );
      }}
    </QueryState>
  );
}
