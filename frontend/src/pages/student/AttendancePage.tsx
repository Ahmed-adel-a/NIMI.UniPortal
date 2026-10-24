import { useCallback, useState, type FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { QrScanner } from '../../components/QrScanner';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { TextField } from '../../components/ui/Field';
import { PageHeader } from '../../components/ui/PageHeader';
import { Alert } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { deviceFingerprint } from '../../lib/device';

/**
 * Check-in: scan the QR code on the lecturer's screen (or paste its code). The first check-in registers this browser as
 * the student's attendance device.
 */
export function AttendancePage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [scanning, setScanning] = useState(false);
  const [token, setToken] = useState('');

  const checkIn = useMutation({
    mutationFn: (code: string) => call(api.POST('/api/attendance/check-in', { body: { token: code.trim(), deviceFingerprint: deviceFingerprint() } })),
    onSuccess: () => setToken(''),
  });
  const scanned = useCallback(
    (value: string) => {
      setScanning(false);
      checkIn.mutate(value);
    },
    [checkIn],
  );

  function submit(event: FormEvent) {
    event.preventDefault();
    checkIn.mutate(token);
  }

  return (
    <>
      <PageHeader title={t('attendance.title')} subtitle={t('attendance.hint')} />
      <Card className="max-w-lg">
        <div className="grid gap-4">
          {scanning ? (
            <>
              <QrScanner onResult={scanned} />
              <Button variant="secondary" onClick={() => setScanning(false)}>{t('app.cancel')}</Button>
            </>
          ) : (
            <Button busy={checkIn.isPending} onClick={() => setScanning(true)}>{t('attendance.scan')}</Button>
          )}
          {checkIn.isSuccess && (
            <Alert tone="success">
              {t('attendance.done', { subject: checkIn.data.subjectCode, status: t(`attendance.statuses.${checkIn.data.status}`, { defaultValue: checkIn.data.status }) })}
            </Alert>
          )}
          {checkIn.isError && <Alert>{errorMessage(checkIn.error)}</Alert>}
          <form className="grid gap-3 border-t border-line pt-4" onSubmit={submit}>
            <TextField label={t('attendance.code')} hint={t('attendance.pasteHint')} value={token} onChange={(e) => setToken(e.target.value)} dir="ltr" autoComplete="off" required />
            <Button type="submit" variant="secondary" busy={checkIn.isPending}>{t('attendance.checkIn')}</Button>
          </form>
          <p className="text-xs text-muted">{t('attendance.device')}</p>
        </div>
      </Card>
    </>
  );
}
