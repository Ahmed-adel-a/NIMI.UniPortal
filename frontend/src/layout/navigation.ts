import type { Role } from '../auth/AuthContext';

export interface NavItem {
  to: string;
  label: string;   // i18n key
  icon: string;    // a single glyph keeps the bundle free of an icon library
}

/** The sidebar for each role. Students on year groups see their standing instead of registration. */
export function navigationFor(role: Role | string, academicSystem?: string): NavItem[] {
  const common: NavItem[] = [
    { to: '/notifications', label: 'nav.notifications', icon: '🔔' },
    { to: '/profile', label: 'nav.profile', icon: '👤' },
  ];
  if (role === 'student') {
    return [
      { to: '/student', label: 'nav.home', icon: '🏠' },
      { to: '/student/schedule', label: 'nav.schedule', icon: '🗓' },
      academicSystem === 'year_groups'
        ? { to: '/student/standing', label: 'nav.standing', icon: '🎓' }
        : { to: '/student/registration', label: 'nav.registration', icon: '📝' },
      { to: '/student/courses', label: 'nav.courses', icon: '📚' },
      { to: '/student/assignments', label: 'nav.assignments', icon: '📤' },
      { to: '/student/grades', label: 'nav.grades', icon: '📊' },
      { to: '/student/attendance', label: 'nav.attendance', icon: '✅' },
      { to: '/student/fees', label: 'nav.fees', icon: '💳' },
      { to: '/student/reports', label: 'nav.reports', icon: '📮' },
      ...common,
    ];
  }
  return [{ to: '/home', label: 'nav.home', icon: '🏠' }, ...common];
}
