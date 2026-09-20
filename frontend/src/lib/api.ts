const BASE = import.meta.env.VITE_API_BASE ?? 'http://localhost:5000';

let currentToken: string | null = null;

export function setToken(token: string | null) {
  currentToken = token;
  if (token) localStorage.setItem('shelly.token', token);
  else localStorage.removeItem('shelly.token');
}

export function loadToken(): string | null {
  const t = localStorage.getItem('shelly.token');
  currentToken = t;
  return t;
}

export async function api<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  if (!headers.has('Content-Type') && init.body) {
    headers.set('Content-Type', 'application/json');
  }
  if (currentToken) headers.set('Authorization', `Bearer ${currentToken}`);

  const res = await fetch(`${BASE}${path}`, { ...init, headers });

  if (res.status === 401) {
    // Token expired or invalid — caller can react.
    throw new ApiError(401, 'unauthorized');
  }
  if (!res.ok) {
    let detail: unknown = null;
    try { detail = await res.json(); } catch { /* not JSON */ }
    throw new ApiError(res.status, `request failed: ${res.status}`, detail);
  }
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export class ApiError extends Error {
  constructor(public status: number, message: string, public detail?: unknown) {
    super(message);
  }
}