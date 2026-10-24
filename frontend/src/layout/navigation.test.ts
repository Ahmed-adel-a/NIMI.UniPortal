import { describe, expect, it } from 'vitest';
import { homeOf } from '../auth/roles';
import { navigationFor } from './navigation';

describe('navigation', () => {
  it('lands every role on a page its menu links to', () => {
    for (const role of ['student', 'professor', 'department_head', 'admin']) {
      expect(navigationFor(role).map((item) => item.to)).toContain(homeOf(role));
    }
  });

  it('gives Student Affairs its own menu', () => {
    const links = navigationFor('admin').map((item) => item.to);
    expect(links).toEqual(expect.arrayContaining(['/admin', '/admin/users', '/admin/grades', '/admin/settings']));
    expect(links.some((to) => to.startsWith('/student') || to.startsWith('/teach'))).toBe(false);
  });

  it('shows year-group students their standing instead of registration', () => {
    const links = navigationFor('student', 'year_groups').map((item) => item.to);
    expect(links).toContain('/student/standing');
    expect(links).not.toContain('/student/registration');
  });
});
