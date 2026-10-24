import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../auth/useAuth';
import { Card } from '../../components/ui/Card';
import { PageHeader } from '../../components/ui/PageHeader';
import { formatDateTime } from '../../lib/format';

export function ProfilePage() {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  if (!user) return null;

  const rows: [string, string | null | undefined][] = [
    [t('profile.fullName'), user.fullName],
    [t('profile.email'), user.email],
    [t('profile.role'), t(`roles.${user.role}`)],
    [t('profile.studentCode'), user.studentCode],
    [t('profile.faculty'), user.facultyCode],
    [t('profile.department'), user.departmentCode],
    [t('profile.enrollmentYear'), user.enrollmentYear?.toString()],
    [t('profile.academicSystem'), t(`profile.systems.${user.academicSystem}`)],
    [t('profile.lastLogin'), formatDateTime(user.lastLoginAt, i18n.language)],
  ];

  return (
    <>
      <PageHeader
        title={t('profile.title')}
        actions={
          <Link to="/change-password" className="rounded-lg border border-line bg-surface px-4 py-2 text-sm font-semibold hover:bg-sunk">
            {t('profile.changePassword')}
          </Link>
        }
      />
      <Card>
        <dl className="grid gap-x-6 gap-y-3 sm:grid-cols-[12rem_1fr]">
          {rows
            .filter(([, value]) => value)
            .map(([label, value]) => (
              <div key={label} className="contents">
                <dt className="text-sm text-muted">{label}</dt>
                <dd className="font-medium">{value}</dd>
              </div>
            ))}
        </dl>
      </Card>
    </>
  );
}
