import { afterEach, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { AuthContext, type AuthState } from '../../auth/AuthContext';
import i18n from '../../i18n';
import { ProfilePage } from './ProfilePage';

const auth: AuthState = {
  session: null,
  loadingUser: false,
  user: {
    id: '0198a1b2-0000-7000-8000-000000000001',
    fullName: 'Omar Khaled',
    email: 'omar.khaled@demo.cu.edu.eg',
    role: 'student',
    studentCode: '2026001',
    academicTitle: null,
    departmentId: null,
    departmentCode: 'CSE',
    facultyId: null,
    facultyCode: 'ENG',
    enrollmentYear: 2026,
    mustChangePassword: false,
    lastLoginAt: null,
    academicSystem: 'credit_hours',
  },
  login: async () => ({ kind: 'signed-in', mustChangePassword: false }),
  logout: async () => {},
  changePassword: async () => {},
};

function renderProfile() {
  render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter>
        <ProfilePage />
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

afterEach(async () => {
  await i18n.changeLanguage('ar');
});

describe('Profile & settings', () => {
  it('switches the whole app between Arabic and English, one language at a time', async () => {
    renderProfile();
    expect(screen.getByRole('heading', { name: 'الملف الشخصي والإعدادات' })).toBeInTheDocument();

    await userEvent.selectOptions(screen.getByLabelText('اللغة'), 'en');

    expect(i18n.language).toBe('en');
    expect(document.documentElement.dir).toBe('ltr');
    expect(screen.getByRole('heading', { name: 'Profile & settings' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Arabic' })).toBeInTheDocument();
    expect(screen.queryByText(/[؀-ۿ]/)).toBeNull(); // no Arabic left on the page

    await userEvent.selectOptions(screen.getByLabelText('Language'), 'ar');

    expect(document.documentElement.dir).toBe('rtl');
    expect(screen.getByRole('option', { name: 'الإنجليزية' })).toBeInTheDocument();
    expect(screen.queryByText(/[A-Za-z]{3,}/, { ignore: '.ltr-nums, [dir="ltr"], dd' })).toBeNull(); // only Arabic labels
  });
});
