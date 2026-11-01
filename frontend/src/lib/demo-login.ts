import { getApiBaseUrl, getKeycloakConfig, getTenantSlugFromPath } from './runtime-env';

export type DemoRole = 'admin' | 'member';

export interface DemoAccount {
  role: DemoRole;
  username: string;
  password: string;
}

export interface DemoSession {
  token: string;
  refreshToken: string;
  idToken?: string;
}

const STORAGE_KEY = 'ruff-demo-session';

// Resolves to the demo accounts only when this tenant is the configured demo tenant.
export const fetchDemoAccounts = async (): Promise<DemoAccount[]> => {
  try {
    const tenantSlug = getTenantSlugFromPath();
    const headers: Record<string, string> = {};
    if (tenantSlug) {
      headers['X-Tenant-Slug'] = tenantSlug;
    }

    const response = await fetch(`${getApiBaseUrl()}/api/demo-login`, { headers });
    if (!response.ok) {
      return [];
    }

    const payload = (await response.json()) as { accounts?: DemoAccount[] };
    return payload.accounts ?? [];
  } catch {
    return [];
  }
};

const tokenEndpoint = (): string => {
  const config = getKeycloakConfig();
  return `${config.url}/realms/${encodeURIComponent(config.realm)}/protocol/openid-connect`;
};

export const requestDemoSession = async (account: DemoAccount): Promise<DemoSession> => {
  const config = getKeycloakConfig();
  const response = await fetch(`${tokenEndpoint()}/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'password',
      client_id: config.clientId,
      username: account.username,
      password: account.password,
      scope: 'openid profile email',
    }),
  });

  if (!response.ok) {
    throw new Error('The demo is unavailable right now. Please try again in a moment.');
  }

  const data = (await response.json()) as {
    access_token: string;
    refresh_token: string;
    id_token?: string;
  };

  return { token: data.access_token, refreshToken: data.refresh_token, idToken: data.id_token };
};

export const storeDemoSession = (session: DemoSession): void => {
  window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
};

export const getStoredDemoSession = (): DemoSession | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  try {
    const raw = window.sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as DemoSession) : null;
  } catch {
    return null;
  }
};

export const clearDemoSession = (): void => {
  if (typeof window !== 'undefined') {
    window.sessionStorage.removeItem(STORAGE_KEY);
  }
};

// Ends the Keycloak session server-side; failures are ignored because the local session is cleared anyway.
export const revokeDemoSession = async (session: DemoSession): Promise<void> => {
  try {
    const config = getKeycloakConfig();
    await fetch(`${tokenEndpoint()}/logout`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({ client_id: config.clientId, refresh_token: session.refreshToken }),
      keepalive: true,
    });
  } catch {
    // Best effort only.
  }
};
