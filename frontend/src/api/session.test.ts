import { describe, expect, it, vi, beforeEach } from 'vitest';
import { getSession, needsRefresh, refreshSession, setSession, type Session } from './session';

function session(overrides: Partial<Session> = {}): Session {
  return {
    accessToken: 'access-1',
    expiresAt: new Date(Date.now() + 10 * 60_000).toISOString(),
    refreshToken: 'refresh-1',
    refreshTokenExpiresAt: new Date(Date.now() + 14 * 86_400_000).toISOString(),
    mustChangePassword: false,
    ...overrides,
  };
}

describe('session', () => {
  beforeEach(() => setSession(null));

  it('needs a refresh shortly before the access token expires', () => {
    const now = new Date('2026-10-24T10:00:00Z');
    expect(needsRefresh(session({ expiresAt: '2026-10-24T10:10:00Z' }), now)).toBe(false);
    expect(needsRefresh(session({ expiresAt: '2026-10-24T10:00:20Z' }), now)).toBe(true);
    expect(needsRefresh(session({ expiresAt: '2026-10-24T09:59:00Z' }), now)).toBe(true);
  });

  it('survives a reload through local storage and clears on sign-out', () => {
    setSession(session());
    expect(JSON.parse(localStorage.getItem('nimi.session')!).accessToken).toBe('access-1');
    setSession(null);
    expect(localStorage.getItem('nimi.session')).toBeNull();
    expect(getSession()).toBeNull();
  });

  it('spends the single-use refresh token once even when several requests ask at the same time', async () => {
    setSession(session());
    const fetcher = vi.fn().mockResolvedValue(
      new Response(JSON.stringify(session({ accessToken: 'access-2', refreshToken: 'refresh-2' })), { status: 200 }),
    );

    const [a, b] = await Promise.all([refreshSession(fetcher), refreshSession(fetcher)]);

    expect(fetcher).toHaveBeenCalledTimes(1);
    expect(a?.accessToken).toBe('access-2');
    expect(b?.accessToken).toBe('access-2');
    expect(getSession()?.refreshToken).toBe('refresh-2');
  });

  it('ends the session when the refresh is refused', async () => {
    setSession(session());
    const fetcher = vi.fn().mockResolvedValue(new Response('{}', { status: 401 }));

    expect(await refreshSession(fetcher)).toBeNull();
    expect(getSession()).toBeNull();
  });

  it('does not call the API with an expired refresh token', async () => {
    setSession(session({ refreshTokenExpiresAt: new Date(Date.now() - 1000).toISOString() }));
    const fetcher = vi.fn();

    expect(await refreshSession(fetcher)).toBeNull();
    expect(fetcher).not.toHaveBeenCalled();
  });
});
