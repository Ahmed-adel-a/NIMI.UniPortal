import createClient, { type Middleware } from 'openapi-fetch';
import type { paths } from './schema';
import { ApiError, isApiErrorBody } from './errors';
import { getSession, needsRefresh, refreshSession, setSession } from './session';

const NO_AUTH = ['/api/auth/login', '/api/auth/refresh', '/api/auth/forgot-password', '/api/auth/reset-password', '/api/platform/settings'];

/** Adds the bearer token, refreshing it first when it is about to expire; a 401 afterwards ends the session. */
const auth: Middleware = {
  async onRequest({ request, schemaPath }) {
    if (NO_AUTH.includes(schemaPath)) return request;
    let session = getSession();
    if (session && needsRefresh(session)) session = await refreshSession();
    if (session) request.headers.set('Authorization', `Bearer ${session.accessToken}`);
    return request;
  },
  onResponse({ response, schemaPath }) {
    if (response.status === 401 && !NO_AUTH.includes(schemaPath)) setSession(null);
    return response;
  },
};

// Same origin as the app (the dev server and the production host both forward /api). fetch is looked up per call,
// so tests can replace it.
export const api = createClient<paths>({
  baseUrl: typeof window === 'undefined' ? '' : window.location.origin,
  fetch: (request) => globalThis.fetch(request),
});
api.use(auth);

/**
 * Awaits an openapi-fetch call and returns its data, or throws an ApiError carrying the error envelope, so React
 * Query sees failures as errors.
 */
export async function call<T>(request: Promise<{ data?: T; error?: unknown; response: Response }>): Promise<T> {
  let result: { data?: T; error?: unknown; response: Response };
  try {
    result = await request;
  } catch {
    throw new ApiError(0, null);   // network error: the server can't be reached
  }
  if (!result.response.ok) throw new ApiError(result.response.status, isApiErrorBody(result.error) ? result.error : null);
  return result.data as T;
}

/** Downloads a protected file (PDF, CSV, uploaded material) with the user's token and saves it under its name. */
export async function downloadFile(url: string, fallbackName: string): Promise<void> {
  let session = getSession();
  if (session && needsRefresh(session)) session = await refreshSession();
  const response = await fetch(url, { headers: session ? { Authorization: `Bearer ${session.accessToken}` } : {} });
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new ApiError(response.status, isApiErrorBody(body) ? body : null);
  }
  const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const name = match ? decodeURIComponent(match[1]) : fallbackName;
  const blob = await response.blob();
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = name;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(link.href), 10_000);
}
