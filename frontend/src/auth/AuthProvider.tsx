import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { api, call } from '../api/client';
import { ApiError } from '../api/errors';
import { getSession, onSessionChange, setSession, type Session } from '../api/session';
import { AuthContext, type AuthState, type LoginResult, type Role } from './AuthContext';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setLocalSession] = useState<Session | null>(getSession);
  const queryClient = useQueryClient();

  useEffect(() => onSessionChange(setLocalSession), []);

  // Signing out (or a dead session) drops every cached answer, so the next user never sees the last one's data.
  useEffect(() => {
    if (!session) queryClient.clear();
  }, [session, queryClient]);

  const me = useQuery({
    queryKey: ['me', session?.accessToken ? 'signed-in' : 'signed-out'],
    queryFn: () => call(api.GET('/api/auth/me')),
    enabled: !!session,
    staleTime: 5 * 60_000,
  });

  const login = useCallback(async (loginName: string, password: string, role?: Role): Promise<LoginResult> => {
    try {
      const tokens = await call(api.POST('/api/auth/login', { body: { email: loginName.trim(), password, role: role ?? null } }));
      setSession(tokens);
      return { kind: 'signed-in', mustChangePassword: tokens.mustChangePassword };
    } catch (error) {
      if (error instanceof ApiError && error.code === 'choice_required')
        return { kind: 'choose-role', roles: (error.body?.details ?? []).map((d) => d.error as Role) };
      throw error;
    }
  }, []);

  const logout = useCallback(async () => {
    const current = getSession();
    if (current) await api.POST('/api/auth/logout', { body: { refreshToken: current.refreshToken } }).catch(() => undefined);
    setSession(null);
  }, []);

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      const tokens = await call(api.POST('/api/auth/change-password', { body: { currentPassword, newPassword } }));
      setSession(tokens);
      await queryClient.invalidateQueries({ queryKey: ['me'] });
    },
    [queryClient],
  );

  const value = useMemo<AuthState>(
    () => ({ session, user: session ? (me.data ?? null) : null, loadingUser: !!session && me.isPending, login, logout, changePassword }),
    [session, me.data, me.isPending, login, logout, changePassword],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
