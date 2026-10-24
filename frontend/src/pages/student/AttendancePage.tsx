import { useState, type FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { TextField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { deviceFingerprint } from '../../lib/device';

/**
 * Check-in by typing the session code shown under the lecturer's QR (camera scanning comes later). The first check-in
 * registers this browser as the student's attendance device.
 */
export function AttendancePage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [token, setToken] = useState('');

  const checkIn = useMutation({
    mutationFn: () => call(api.POST('/api/attendance/check-in', { body: { token: token.trim(), deviceFingerprint: deviceFingerprint() } })),
    onSuccess: () => setToken(''),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    checkIn.mutate();
  }

  return (
    <>
      <PageHeader title={t('attendance.title')} subtitle={t('attendance.hint')} />
      <Card className="max-w-lg">
        <form className="grid gap-4" onSubmit={submit}>
          <TextField label={t('attendance.code')} value={token} onChange={(e) => setToken(e.target.value)} dir="ltr" autoComplete="off" required />
          {checkIn.isSuccess && (
            <Alert tone="success">
              {t('attendance.done', { subject: checkIn.data.subjectCode, status: t(`attendance.statuses.${checkIn.data.status}`, { defaultValue: checkIn.data.status }) })}
            </Alert>
          )}
          {checkIn.isError && <Alert>{errorMessage(checkIn.error)}</Alert>}
          <Button type="submit" busy={checkIn.isPending}>
            {t('attendance.checkIn')}
          </Button>
          <p className="text-xs text-muted">{t('attendance.device')}</p>
        </form>
      </Card>
    </>
  );
}
