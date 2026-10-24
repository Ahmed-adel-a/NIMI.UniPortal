import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './useAuth';
import type { Role } from './AuthContext';
import { FullPageSpinner } from '../components/ui/Spinner';

/**
 * Guards a route: no session → sign-in; a temporary password → the change-password page; a role the route isn't for
 * → the user's own home.
 */
export function RequireAuth({ roles, children }: { roles?: Role[]; children: ReactNode }) {
  const { session, user, loadingUser } = useAuth();
  const location = useLocation();

  if (!session) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (session.mustChangePassword && location.pathname !== '/change-password') return <Navigate to="/change-password" replace />;
  if (loadingUser || !user) return <FullPageSpinner />;
  if (roles && !roles.includes(user.role as Role)) return <Navigate to="/" replace />;
  return <>{children}</>;
}
