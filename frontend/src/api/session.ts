import type { components } from './schema';

export type TokenResponse = components['schemas']['TokenResponse'];

/** What the browser keeps between visits: the short-lived access token and the rotating refresh token. */
export interface Session {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  mustChangePassword: boolean;
}

const STORAGE_KEY = 'nimi.session';
const listeners = new Set<(session: Session | null) => void>();
let current: Session | null = read();

function read(): Session | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    return null;
  }
}

export function getSession(): Session | null {
  return current;
}

export function setSession(tokens: TokenResponse | Session | null) {
  current = tokens
    ? {
        accessToken: tokens.accessToken,
        expiresAt: tokens.expiresAt,
        refreshToken: tokens.refreshToken,
        refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
        mustChangePassword: tokens.mustChangePassword,
      }
    : null;
  try {
    if (current) localStorage.setItem(STORAGE_KEY, JSON.stringify(current));
    else localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Without storage the session lasts until the tab closes.
  }
  listeners.forEach((listener) => listener(current));
}

export function onSessionChange(listener: (session: Session | null) => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

/** Refresh a little before the access token expires, so a request never goes out with a dead token. */
export function needsRefresh(session: Session, now: Date = new Date(), marginSeconds = 30): boolean {
  return new Date(session.expiresAt).getTime() - now.getTime() < marginSeconds * 1000;
}

export function refreshExpired(session: Session, now: Date = new Date()): boolean {
  return new Date(session.refreshTokenExpiresAt).getTime() <= now.getTime();
}

let inFlight: Promise<Session | null> | null = null;

/**
 * Exchanges the refresh token for a new pair. One exchange at a time: the refresh token is single-use, so parallel
 * requests share the same exchange instead of each spending (and invalidating) it.
 */
export function refreshSession(fetcher: typeof fetch = fetch): Promise<Session | null> {
  const session = current;
  if (!session || refreshExpired(session)) {
    setSession(null);
    return Promise.resolve(null);
  }
  inFlight ??= (async () => {
    try {
      const response = await fetcher('/api/auth/refresh', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken: session.refreshToken }),
      });
      if (!response.ok) {
        setSession(null);
        return null;
      }
      setSession((await response.json()) as TokenResponse);
      return current;
    } catch {
      return current;   // offline: keep the session and let the request fail on its own
    } finally {
      inFlight = null;
    }
  })();
  return inFlight;
}
