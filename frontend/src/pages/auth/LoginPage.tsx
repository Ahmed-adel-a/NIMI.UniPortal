import { useState, type FormEvent } from 'react';
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../auth/useAuth';
import type { Role } from '../../auth/AuthContext';
import { ApiError } from '../../api/errors';
import { Button } from '../../components/ui/Button';
import { TextField } from '../../components/ui/Field';
import { Alert } from '../../components/ui/States';
import { useErrorMessage } from '../../components/ui/useErrorMessage';
import { AuthLayout } from './AuthLayout';

export function LoginPage() {
  const { t } = useTranslation();
  const { session, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const errorMessage = useErrorMessage();
  const [loginName, setLoginName] = useState('');
  const [password, setPassword] = useState('');
  const [roles, setRoles] = useState<Role[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (session && !busy) return <Navigate to={session.mustChangePassword ? '/change-password' : '/'} replace />;

  async function signIn(role?: Role) {
    setBusy(true);
    setError(null);
    try {
      const result = await login(loginName, password, role);
      if (result.kind === 'choose-role') {
        setRoles(result.roles);
        return;
      }
      const from = (location.state as { from?: string } | null)?.from;
      navigate(result.mustChangePassword ? '/change-password' : (from ?? '/'), { replace: true });
    } catch (caught) {
      setRoles(null);
      setError(caught instanceof ApiError && caught.status === 401 ? t('auth.invalid') : errorMessage(caught));
    } finally {
      setBusy(false);
    }
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    void signIn();
  }

  return (
    <AuthLayout title={t('auth.title')} subtitle={t('auth.subtitle')}>
      <form className="grid gap-4" onSubmit={submit}>
        <TextField
          label={t('auth.login')}
          value={loginName}
          onChange={(e) => {
            setLoginName(e.target.value);
            setRoles(null);
          }}
          autoComplete="username"
          inputMode={/^\d*$/.test(loginName) ? 'numeric' : 'email'}
          dir="ltr"
          required
        />
        <TextField
          label={t('auth.password')}
          type="password"
          value={password}
          onChange={(e) => {
            setPassword(e.target.value);
            setRoles(null);
          }}
          autoComplete="current-password"
          dir="ltr"
          required
        />
        {error && <Alert>{error}</Alert>}
        {roles ? (
          <fieldset className="grid gap-2">
            <legend className="mb-2 text-sm">{t('auth.chooseRole')}</legend>
            {roles.map((role) => (
              <Button key={role} variant="secondary" busy={busy} onClick={() => void signIn(role)}>
                {t(`roles.${role}`)}
              </Button>
            ))}
          </fieldset>
        ) : (
          <Button type="submit" busy={busy}>
            {t('auth.submit')}
          </Button>
        )}
        <Link to="/forgot-password" className="text-center text-sm text-primary hover:underline">
          {t('auth.forgot')}
        </Link>
      </form>
    </AuthLayout>
  );
}
