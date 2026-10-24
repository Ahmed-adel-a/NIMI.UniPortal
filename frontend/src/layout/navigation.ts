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
  if (role === 'professor') return [{ to: '/teach', label: 'nav.teaching', icon: '🧑‍🏫' }, ...common];
  if (role === 'department_head') {
    return [
      { to: '/head/reports', label: 'nav.headReports', icon: '📮' },
      { to: '/teach', label: 'nav.teaching', icon: '🧑‍🏫' },
      ...common,
    ];
  }
  if (role === 'admin') {
    return [
      { to: '/admin', label: 'admin.nav.dashboard', icon: '📈' },
      { to: '/admin/users', label: 'admin.nav.users', icon: '👥' },
      { to: '/admin/organization', label: 'admin.nav.organization', icon: '🏛' },
      { to: '/admin/terms', label: 'admin.nav.terms', icon: '📆' },
      { to: '/admin/subjects', label: 'admin.nav.subjects', icon: '📚' },
      { to: '/admin/facilities', label: 'admin.nav.facilities', icon: '🏫' },
      { to: '/admin/timetable', label: 'admin.nav.timetable', icon: '🗓' },
      { to: '/admin/grades', label: 'admin.nav.grades', icon: '📊' },
      { to: '/admin/payments', label: 'admin.nav.payments', icon: '💳' },
      { to: '/admin/reports', label: 'admin.nav.reports', icon: '📮' },
      { to: '/admin/devices', label: 'admin.nav.devices', icon: '📱' },
      { to: '/admin/activity', label: 'admin.nav.activity', icon: '🧾' },
      { to: '/admin/settings', label: 'admin.nav.settings', icon: '⚙️' },
      ...common,
    ];
  }
  return [{ to: '/home', label: 'nav.home', icon: '🏠' }, ...common];
}
