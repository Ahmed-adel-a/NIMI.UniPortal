import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call, downloadFile, sendForm } from '../../../api/client';
import type { Schema } from '../../../api/types';
import { Button } from '../../../components/ui/Button';
import { Card } from '../../../components/ui/Card';
import { TextField } from '../../../components/ui/Field';
import { Alert, EmptyState, QueryState } from '../../../components/ui/States';
import { useErrorMessage } from '../../../components/ui/useErrorMessage';
import { formatDate } from '../../../lib/format';
import type { TeachingClass } from '../ClassPage';

export function MaterialsTab({ current }: { current: TeachingClass }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const errorMessage = useErrorMessage();
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [week, setWeek] = useState('');
  const [thisGroup, setThisGroup] = useState(false);
  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const key = ['materials', current.subjectId];

  const materials = useQuery({
    queryKey: key,
    queryFn: () => call(api.GET('/api/materials', { params: { query: { subjectId: current.subjectId, pageSize: 100 } } })),
  });
  const upload = useMutation({
    mutationFn: () => {
      const form = new FormData();
      form.append('file', file!);
      form.append('subjectId', current.subjectId);
      if (thisGroup) form.append('subjectGroupId', current.groupId);
      form.append('title', title);
      if (description) form.append('description', description);
      if (week) form.append('weekNumber', week);
      return sendForm<Schema<'MaterialDto'>>('/api/materials', form);
    },
    onSuccess: () => {
      setFile(null);
      setTitle('');
      setDescription('');
      setWeek('');
      setMessage({ tone: 'success', text: t('teach.materials.uploaded') });
      void queryClient.invalidateQueries({ queryKey: key });
    },
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });
  const remove = useMutation({
    mutationFn: (id: string) => call(api.DELETE('/api/materials/{id}', { params: { path: { id } } })),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: key }),
    onError: (e) => setMessage({ tone: 'danger', text: errorMessage(e) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    upload.mutate();
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[22rem_1fr]">
      <Card title={t('teach.materials.upload')}>
        <form className="grid gap-3" onSubmit={submit}>
          <input type="file" required aria-label={t('teach.materials.file')} onChange={(e) => setFile(e.target.files?.[0] ?? null)} className="text-sm file:me-3 file:rounded-lg file:border-0 file:bg-sunk file:px-3 file:py-2" />
          <TextField label={t('teach.materials.title')} value={title} onChange={(e) => setTitle(e.target.value)} required />
          <TextField label={t('teach.materials.description')} value={description} onChange={(e) => setDescription(e.target.value)} />
          <TextField label={t('teach.materials.week')} type="number" min={1} max={20} value={week} onChange={(e) => setWeek(e.target.value)} dir="ltr" />
          <label className="flex items-center gap-2 text-sm">
            <input type="checkbox" checked={thisGroup} onChange={(e) => setThisGroup(e.target.checked)} />
            {t('teach.announcements.thisGroup')}
          </label>
          {message && <Alert tone={message.tone}>{message.text}</Alert>}
          <Button type="submit" busy={upload.isPending} disabled={!file}>{t('teach.materials.upload')}</Button>
        </form>
      </Card>
      <QueryState query={materials}>
        {(data) =>
          data.items.length === 0 ? (
            <EmptyState>{t('courses.noMaterials')}</EmptyState>
          ) : (
            <ul className="grid content-start gap-2">
              {data.items.map((m) => (
                <li key={m.id} className="flex flex-wrap items-center justify-between gap-2 rounded-lg border border-line px-3 py-2">
                  <div className="min-w-0">
                    <p className="font-medium">{m.title}</p>
                    <p className="text-xs text-muted">
                      {m.weekNumber ? `${t('courses.week', { n: m.weekNumber })} · ` : ''}
                      <span className="ltr-nums">{m.fileName}</span> · {t('teach.materials.downloads', { n: m.downloadCount })} · {formatDate(m.createdAt, i18n.language)}
                    </p>
                  </div>
                  <span className="flex gap-1">
                    <Button variant="ghost" className="min-h-8 px-2" onClick={() => void downloadFile(`/api/materials/${m.id}/download`, m.fileName)}>{t('app.download')}</Button>
                    <Button variant="ghost" className="min-h-8 px-2 text-danger" onClick={() => { if (window.confirm(t('teach.materials.confirmDelete'))) remove.mutate(m.id); }}>
                      {t('teach.materials.delete')}
                    </Button>
                  </span>
                </li>
              ))}
            </ul>
          )
        }
      </QueryState>
    </div>
  );
}
