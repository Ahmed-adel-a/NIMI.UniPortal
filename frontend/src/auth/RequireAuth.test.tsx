import { describe, expect, it, vi, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './AuthProvider';
import { RequireAuth } from './RequireAuth';
import { setSession } from '../api/session';
import '../i18n';

function signIn(mustChangePassword = false) {
  const later = new Date(Date.now() + 600_000).toISOString();
  setSession({ accessToken: 'a', expiresAt: later, refreshToken: 'r', refreshTokenExpiresAt: later, mustChangePassword });
}

function me(role: string) {
  return new Response(JSON.stringify({ id: '1', fullName: 'Test', email: 't@cu.edu.eg', role, academicSystem: 'credit_hours', mustChangePassword: false }), {
    status: 200,
    headers: { 'Content-Type': 'application/json' },
  });
}

function renderAt(path: string) {
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <MemoryRouter initialEntries={[path]}>
        <AuthProvider>
          <Routes>
            <Route path="/login" element={<p>login page</p>} />
            <Route path="/change-password" element={<p>change password page</p>} />
            <Route path="/" element={<p>role home</p>} />
            <Route path="/student" element={<RequireAuth roles={['student']}><p>student page</p></RequireAuth>} />
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

describe('RequireAuth', () => {
  it('sends a visitor without a session to sign in', () => {
    renderAt('/student');
    expect(screen.getByText('login page')).toBeInTheDocument();
  });

  it('holds a temporary password at the change-password page', () => {
    signIn(true);
    renderAt('/student');
    expect(screen.getByText('change password page')).toBeInTheDocument();
  });

  it('lets the right role in and sends other roles home', async () => {
    signIn();
    vi.stubGlobal('fetch', vi.fn(async () => me('student')));
    renderAt('/student');
    expect(await screen.findByText('student page')).toBeInTheDocument();
  });

  it('sends a professor away from student pages', async () => {
    signIn();
    vi.stubGlobal('fetch', vi.fn(async () => me('professor')));
    renderAt('/student');
    expect(await screen.findByText('role home')).toBeInTheDocument();
  });
});
