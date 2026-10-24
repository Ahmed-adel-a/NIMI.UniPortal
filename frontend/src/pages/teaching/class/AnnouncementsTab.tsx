import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../../api/client';
import { Badge } from '../../../components/ui/Badge';
import { Button } from '../../../components/ui/Button';
import { Card } from '../../../components/ui/Card';
import { SelectField, TextArea, TextField } from '../../../components/ui/Field';
import { Alert, EmptyState, QueryState } from '../../../components/ui/States';
import { useErrorMessage } from '../../../components/ui/useErrorMessage';
import { formatDate } from '../../../lib/format';
import type { TeachingClass } from '../ClassPage';

type Priority = 'normal' | 'high' | 'urgent';

export function AnnouncementsTab({ current }: { current: TeachingClass }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [priority, setPriority] = useState<Priority>('normal');
  const [isPinned, setPinned] = useState(false);
  const [thisGroup, setThisGroup] = useState(false);
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const key = ['announcements', current.subjectId];

  const list = useQuery({
    queryKey: key,
    queryFn: () => call(api.GET('/api/announcements', { params: { query: { subjectId: current.subjectId, pageSize: 50 } } })),
  });
  const post = useMutation({
    mutationFn: () =>
      call(api.POST('/api/announcements', {
        body: { subjectId: current.subjectId, subjectGroupId: thisGroup ? current.groupId : null, title, body, priority, isPinned, expiresAt: null },
      })),
    onSuccess: () => {
      setTitle('');
      setBody('');
      setMessage({ tone: 'success', text: t('teach.announcements.posted') });
      void queryClient.invalidateQueries({ queryKey: key });
    },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });
  const remove = useMutation({
    mutationFn: (id: string) => call(api.DELETE('/api/announcements/{id}', { params: { path: { id } } })),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: key }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    post.mutate();
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[22rem_1fr]">
      <Card title={t('teach.announcements.new')}>
        <form className="grid gap-3" onSubmit={submit}>
          <TextField label={t('teach.announcements.title')} value={title} onChange={(e) => setTitle(e.target.value)} required />
          <TextArea label={t('teach.announcements.body')} value={body} onChange={(e) => setBody(e.target.value)} required />
          <SelectField label={t('teach.announcements.priority')} value={priority} onChange={(e) => setPriority(e.target.value as Priority)}>
            {(['normal', 'high', 'urgent'] as const).map((p) => <option key={p} value={p}>{t(`courses.priority.${p}`)}</option>)}
          </SelectField>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={isPinned} onChange={(e) => setPinned(e.target.checked)} />{t('teach.announcements.pinned')}</label>
          <label className="flex items-center gap-2 text-sm"><input type="checkbox" checked={thisGroup} onChange={(e) => setThisGroup(e.target.checked)} />{t('teach.announcements.thisGroup')}</label>
          {message && <Alert tone={message.tone}>{message.text}</Alert>}
          <Button type="submit" busy={post.isPending}>{t('app.send')}</Button>
        </form>
      </Card>
      <QueryState query={list}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('courses.noAnnouncements')}</EmptyState>
          ) : (
            <ul className="grid content-start gap-3">
              {data.items.map((a) => (
                <li key={a.id} className="rounded-lg border border-line p-3">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="flex flex-wrap items-center gap-2">
                      <h3 className="font-semibold">{a.title}</h3>
                      {a.isPinned && <Badge tone="accent">{t('courses.pinned')}</Badge>}
                      {a.priority !== 'normal' && <Badge tone={a.priority === 'urgent' ? 'danger' : 'warning'}>{t(`courses.priority.${a.priority}`)}</Badge>}
                    </span>
                    <Button variant="ghost" className="min-h-8 px-2 text-danger" onClick={() => { if (window.confirm(t('teach.announcements.confirmDelete'))) remove.mutate(a.id); }}>
                      {t('teach.announcements.delete')}
                    </Button>
                  </div>
                  <p className="mt-1 whitespace-pre-line text-sm">{a.body}</p>
                  <p className="mt-2 text-xs text-muted">{a.authorName} · {formatDate(a.createdAt, i18n.language)}</p>
                </li>
              ))}
            </ul>
          )
        }
      </QueryState>
    </div>
  );
}
