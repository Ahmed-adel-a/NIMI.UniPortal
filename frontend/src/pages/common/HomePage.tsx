import { useTranslation } from 'react-i18next';
import { useAuth } from '../../auth/useAuth';
import { Alert } from '../../components/ui/States';
import { PageHeader } from '../../components/ui/PageHeader';

/** Teaching staff, department heads and Student Affairs land here until their pages arrive (F2, F3). */
export function HomePage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  return (
    <>
      <PageHeader title={t('dashboard.greeting', { name: user?.fullName ?? '' })} subtitle={user ? t(`roles.${user.role}`) : undefined} />
      <Alert tone="info">{t('home.staffSoon')}</Alert>
    </>
  );
}
