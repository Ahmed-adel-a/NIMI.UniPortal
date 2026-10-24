import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Button } from '../../components/ui/Button';
import { PageHeader } from '../../components/ui/PageHeader';
import { EmptyState, QueryState } from '../../components/ui/States';
import { formatDateTime } from '../../lib/format';
import { Pager } from '../../components/ui/Pager';

export function NotificationsPage() {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [page, setPage] = useState(1);
  const ar = i18n.language === 'ar';

  const list = useQuery({
    queryKey: ['notifications', unreadOnly, page],
    queryFn: () => call(api.GET('/api/notifications/mine', { params: { query: { unreadOnly, page, pageSize: 20 } } })),
  });
  const refresh = () => queryClient.invalidateQueries({ queryKey: ['notifications'] });
  const readAll = useMutation({ mutationFn: () => call(api.POST('/api/notifications/read-all')), onSuccess: refresh });
  const read = useMutation({ mutationFn: (id: string) => call(api.POST('/api/notifications/{id}/read', { params: { path: { id } } })), onSuccess: refresh });

  return (
    <>
      <PageHeader
        title={t('notifications.title')}
        actions={
          <>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={unreadOnly} onChange={(e) => { setUnreadOnly(e.target.checked); setPage(1); }} />
              {t('notifications.unreadOnly')}
            </label>
            <Button variant="secondary" busy={readAll.isPending} onClick={() => readAll.mutate()}>
              {t('notifications.markAll')}
            </Button>
          </>
        }
      />
      <QueryState query={list}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('notifications.empty')}</EmptyState>
          ) : (
            <div className="grid gap-2">
              {data.items.map((n) => (
                <article key={n.id} className={`rounded-xl border bg-surface p-4 ${n.isRead ? 'border-line' : 'border-primary/40'}`}>
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <h2 className="font-semibold">{ar ? n.titleAr || n.title : n.title}</h2>
                    <time className="text-xs text-muted">{formatDateTime(n.createdAt, i18n.language)}</time>
                  </div>
                  <p className="mt-1 text-sm text-muted">{ar ? n.messageAr || n.message : n.message}</p>
                  <div className="mt-3 flex gap-2">
                    {!n.isRead && (
                      <Button variant="ghost" className="min-h-8 px-2" onClick={() => read.mutate(n.id)}>
                        {t('notifications.markRead')}
                      </Button>
                    )}
                    {n.actionUrl && (
                      <Button variant="ghost" className="min-h-8 px-2" onClick={() => { if (!n.isRead) read.mutate(n.id); navigate(routeFor(n.actionUrl!)); }}>
                        {t('app.open')}
                      </Button>
                    )}
                  </div>
                </article>
              ))}
              <Pager page={page} totalPages={Number(data.totalPages)} onPage={setPage} />
            </div>
          )
        }
      </QueryState>
    </>
  );
}

/** Notification links are API-style paths (/reports/{id}); map the known ones to app pages. */
function routeFor(actionUrl: string): string {
  if (actionUrl.startsWith('/reports')) return '/student/reports';
  if (actionUrl.startsWith('/announcements')) return '/student/courses';
  if (actionUrl.startsWith('/assignments') || actionUrl.startsWith('/submissions')) return '/student/assignments';
  if (actionUrl.startsWith('/grades')) return '/student/grades';
  if (actionUrl.startsWith('/payments')) return '/student/fees';
  return '/';
}
