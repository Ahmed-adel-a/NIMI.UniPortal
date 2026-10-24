import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../auth/useAuth';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/Field';
import { Alert } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { AuthLayout } from './AuthLayout';

export function ChangePasswordPage() {
  const { t } = useTranslation();
  const { session, changePassword } = useAuth();
  const navigate = useNavigate();
  const errorMessage = useErrorMessage();
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (next !== confirm) {
      setError(t('auth.mismatch'));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      await changePassword(current, next);
      navigate('/', { replace: true });
    } catch (caught) {
      setError(errorMessage(caught));
    } finally {
      setBusy(false);
    }
  }

  return (
    <AuthLayout title={t('auth.changeTitle')} subtitle={session?.mustChangePassword ? t('auth.changeForced') : undefined}>
      <form className="grid gap-4" onSubmit={submit}>
        <TextField label={t('auth.current')} type="password" value={current} onChange={(e) => setCurrent(e.target.value)} autoComplete="current-password" dir="ltr" required />
        <TextField label={t('auth.newPassword')} type="password" value={next} onChange={(e) => setNext(e.target.value)} autoComplete="new-password" dir="ltr" hint={t('auth.rule')} required />
        <TextField label={t('auth.confirm')} type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} autoComplete="new-password" dir="ltr" required />
        {error && <Alert>{error}</Alert>}
        <Button type="submit" busy={busy}>
          {t('app.save')}
        </Button>
      </form>
    </AuthLayout>
  );
}
