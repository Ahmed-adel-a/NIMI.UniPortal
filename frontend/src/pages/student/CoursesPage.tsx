import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile } from '../../api/client';
import { Badge } from '../../components/ui/Badge';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { EmptyState, QueryState } from '../../components/ui/States';
import { formatDate } from '../../lib/format';
import { useMyCourses } from './useMyCourses';

export function CoursesPage() {
  const { t, i18n } = useTranslation();
  const ar = i18n.language === 'ar';
  const courses = useMyCourses();
  const [picked, setPicked] = useState<string | null>(null);
  const subjectId = picked ?? courses.data?.[0]?.subjectId ?? null;

  const materials = useQuery({
    queryKey: ['materials', subjectId],
    queryFn: () => call(api.GET('/api/materials', { params: { query: { subjectId: subjectId!, pageSize: 100 } } })),
    enabled: !!subjectId,
  });
  const announcements = useQuery({
    queryKey: ['announcements', subjectId],
    queryFn: () => call(api.GET('/api/announcements', { params: { query: { subjectId: subjectId!, pageSize: 50 } } })),
    enabled: !!subjectId,
  });

  return (
    <>
      <PageHeader title={t('courses.title')} />
      <QueryState query={courses}>
        {(list) =>
          list.length === 0 ? (
            <EmptyState>{t('courses.none')}</EmptyState>
          ) : (
            <div className="grid gap-6 lg:grid-cols-[16rem_1fr]">
              <nav aria-label={t('courses.pick')} className="grid content-start gap-1">
                {list.map((c) => (
                  <button
                    key={c.subjectId}
                    onClick={() => setPicked(c.subjectId)}
                    className={`rounded-lg px-3 py-2 text-start text-sm transition ${c.subjectId === subjectId ? 'bg-primary-soft font-semibold text-primary' : 'hover:bg-sunk'}`}
                  >
                    <span className="block">{ar ? c.subjectNameAr : c.subjectName}</span>
                    <span className="text-xs text-muted ltr-nums">
                      {c.subjectCode} · G{c.groupNumber}
                    </span>
                  </button>
                ))}
              </nav>
              <div className="grid min-w-0 content-start gap-6">
                <Card title={t('courses.announcements')}>
                  <QueryState query={announcements}>
                    {(data) =>
                      data.items.length === 0 ? (
                        <EmptyState>{t('courses.noAnnouncements')}</EmptyState>
                      ) : (
                        <ul className="grid gap-3">
                          {data.items.map((a) => (
                            <li key={a.id} className="rounded-lg border border-line p-3">
                              <div className="flex flex-wrap items-center gap-2">
                                <h3 className="font-semibold">{a.title}</h3>
                                {a.isPinned && <Badge tone="accent">{t('courses.pinned')}</Badge>}
                                {a.priority !== 'normal' && <Badge tone={a.priority === 'urgent' ? 'danger' : 'warning'}>{t(`courses.priority.${a.priority}`)}</Badge>}
                              </div>
                              <p className="mt-1 whitespace-pre-line text-sm">{a.body}</p>
                              <p className="mt-2 text-xs text-muted">
                                {a.authorName} · {formatDate(a.createdAt, i18n.language)}
                              </p>
                            </li>
                          ))}
                        </ul>
                      )
                    }
                  </QueryState>
                </Card>
                <Card title={t('courses.materials')}>
                  <QueryState query={materials}>
                    {(data) =>
                      data.items.length === 0 ? (
                        <EmptyState>{t('courses.noMaterials')}</EmptyState>
                      ) : (
                        <ul className="grid gap-2">
                          {data.items.map((m) => (
                            <li key={m.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-sunk px-3 py-2">
                              <div className="min-w-0">
                                <p className="font-medium">{m.title}</p>
                                <p className="text-xs text-muted">
                                  {m.weekNumber ? `${t('courses.week', { n: m.weekNumber })} · ` : ''}
                                  <span className="ltr-nums">{m.fileName}</span> · {(m.sizeBytes / 1_048_576).toFixed(1)} MB
                                </p>
                              </div>
                              <Button variant="secondary" className="min-h-8" onClick={() => void downloadFile(`/api/materials/${m.id}/download`, m.fileName)}>
                                {t('app.download')}
                              </Button>
                            </li>
                          ))}
                        </ul>
                      )
                    }
                  </QueryState>
                </Card>
              </div>
            </div>
          )
        }
      </QueryState>
    </>
  );
}
