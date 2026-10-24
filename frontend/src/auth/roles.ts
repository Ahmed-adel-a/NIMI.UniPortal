import type { Role } from './AuthContext';

/** Where each role lands after signing in. */
export function homeOf(role: Role | string | undefined): string {
  switch (role) {
    case 'student':
      return '/student';
    default:
      return '/home';
  }
}
