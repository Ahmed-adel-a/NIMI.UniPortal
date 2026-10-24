import { describe, expect, it, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from '../../auth/AuthProvider';
import { setSession } from '../../api/session';
import { LoginPage } from './LoginPage';
import '../../i18n';

function tokens(mustChangePassword = false) {
  const later = new Date(Date.now() + 600_000).toISOString();
  return { accessToken: 'a', expiresAt: later, refreshToken: 'r', refreshTokenExpiresAt: later, mustChangePassword };
}

function json(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function renderLogin() {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={['/login']}>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route path="/" element={<p>home page</p>} />
            <Route path="/change-password" element={<p>change password page</p>} />
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

afterEach(() => {
  vi.unstubAllGlobals();
  setSession(null);
});

describe('LoginPage', () => {
  it('asks which role to use when the national ID has several accounts, then signs in with it', async () => {
    const fetchMock = vi.fn(async (input: Request) => {
      const body = await input.clone().json();
      if (!body.role)
        return json(409, { code: 'choice_required', message: 'Choose.', details: [{ field: 'role', error: 'professor' }, { field: 'role', error: 'student' }] });
      return json(200, tokens());
    });
    vi.stubGlobal('fetch', fetchMock);
    renderLogin();

    await userEvent.type(screen.getByLabelText(/الرقم القومي/), '29901011234567');
    await userEvent.type(screen.getByLabelText(/كلمة المرور/), '1234567');
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }));

    await userEvent.click(await screen.findByRole('button', { name: 'عضو هيئة تدريس' }));

    expect(await screen.findByText('home page')).toBeInTheDocument();
    const sent = await (fetchMock.mock.calls[1][0] as Request).json();
    expect(sent).toMatchObject({ email: '29901011234567', role: 'professor' });
  });

  it('sends a temporary password straight to the change-password page', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => json(200, tokens(true))));
    renderLogin();

    await userEvent.type(screen.getByLabelText(/الرقم القومي/), 'new.student@cu.edu.eg');
    await userEvent.type(screen.getByLabelText(/كلمة المرور/), 'Temp1234');
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }));

    expect(await screen.findByText('change password page')).toBeInTheDocument();
  });

  it('shows one message for wrong details without saying which part was wrong', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => json(401, { code: 'invalid_credentials', message: 'Invalid email or password.', details: [] })));
    renderLogin();

    await userEvent.type(screen.getByLabelText(/الرقم القومي/), '29901011234567');
    await userEvent.type(screen.getByLabelText(/كلمة المرور/), 'wrong');
    await userEvent.click(screen.getByRole('button', { name: 'دخول' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('بيانات الدخول غير صحيحة');
  });
});
