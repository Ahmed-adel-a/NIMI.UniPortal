import { useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { api, call } from '../../api/client';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/Field';
import { Alert } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { AuthLayout } from './AuthLayout';

export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const errorMessage = useErrorMessage();
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await call(api.POST('/api/auth/forgot-password', { body: { email } }));
      setSent(true);
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout title={t('auth.forgotTitle')} subtitle={t('auth.forgotHint')}>
      <form className="grid gap-4" onSubmit={submit}>
        <TextField label={t('auth.email')} type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="email" dir="ltr" required />
        {sent && <Alert tone="success">{t('auth.forgotSent')}</Alert>}
        {error && <Alert>{error}</Alert>}
        <Button type="submit" busy={busy}>
          {t('app.send')}
        </Button>
        <Link to="/login" className="text-center text-sm text-primary hover:underline">
          {t('auth.backToLogin')}
        </Link>
      </form>
    </AuthLayout>
  );
}
