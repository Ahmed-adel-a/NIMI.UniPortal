import { useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/Field';
import { Alert } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { AuthLayout } from './AuthLayout';

/** The page the "forgot password" email links to: /reset-password?token=… */
export function ResetPasswordPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [params] = useSearchParams();
  const [password, setPassword] = useState('');
  const [confirm, setConfirm] = useState('');
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (password !== confirm) {
      setError(t('auth.mismatch'));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await call(api.POST('/api/auth/reset-password', { body: { token: params.get('token') ?? '', newPassword: password } }));
      setDone(true);
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout title={t('auth.resetTitle')}>
      {done ? (
        <div className="grid gap-4">
          <Alert tone="success">{t('auth.resetDone')}</Alert>
          <Link to="/login" className="text-center text-sm text-primary hover:underline">
            {t('auth.backToLogin')}
          </Link>
        </div>
      ) : (
        <form className="grid gap-4" onSubmit={submit}>
          <TextField label={t('auth.newPassword')} type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="new-password" dir="ltr" hint={t('auth.rule')} required />
          <TextField label={t('auth.confirm')} type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} autoComplete="new-password" dir="ltr" required />
          {error && <Alert>{error}</Alert>}
          <Button type="submit" busy={busy}>
            {t('app.save')}
          </Button>
        </form>
      )}
    </AuthLayout>
  );
}
