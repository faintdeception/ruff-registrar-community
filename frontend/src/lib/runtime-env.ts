export type RuntimeEnv = {
  NEXT_PUBLIC_KEYCLOAK_URL?: string;
  NEXT_PUBLIC_KEYCLOAK_REALM?: string;
  NEXT_PUBLIC_KEYCLOAK_CLIENT_ID?: string;
  NEXT_PUBLIC_API_URL?: string;
  NEXT_PUBLIC_APP_VERSION?: string;
};

type RuntimeWindow = Window & {
  __ENV__?: RuntimeEnv;
};

export const getRuntimeEnv = (): RuntimeEnv | undefined => {
  if (typeof window === 'undefined') {
    return undefined;
  }

  return (window as RuntimeWindow).__ENV__;
};

export const getApiBaseUrl = (): string => {
  if (typeof window === 'undefined') {
    return process.env.API_BASE_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
  }

  return getRuntimeEnv()?.NEXT_PUBLIC_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
};

export const getAppVersion = (): string => {
  if (typeof window === 'undefined') {
    return process.env.NEXT_PUBLIC_APP_VERSION || 'unknown';
  }

  return getRuntimeEnv()?.NEXT_PUBLIC_APP_VERSION || process.env.NEXT_PUBLIC_APP_VERSION || 'unknown';
};

export const getKeycloakConfig = () => {
  const runtimeEnv = getRuntimeEnv();

  const defaultRealm =
    runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_REALM ||
    process.env.NEXT_PUBLIC_KEYCLOAK_REALM ||
    'student-registrar';

  const tenantRealm = getTenantRealmFromPath();

  return {
    url: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_URL || process.env.NEXT_PUBLIC_KEYCLOAK_URL || 'http://localhost:8080',
    realm: tenantRealm || defaultRealm,
    clientId: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'student-registrar-spa',
  };
};

const getTenantRealmFromPath = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  const tenantSlug = getTenantSlugFromPath();
  if (!tenantSlug) {
    return null;
  }

  return `${tenantSlug}-org`;
};

export const getTenantSlugFromPath = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  const segments = window.location.pathname
    .split('/')
    .map(segment => segment.trim().toLowerCase())
    .filter(Boolean);

  const candidate = segments[0];
  if (!candidate || ReservedPathSegments.has(candidate) || !isValidTenantSlug(candidate)) {
    return null;
  }

  return candidate;
};

const isValidTenantSlug = (value: string): boolean => {
  return /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/.test(value);
};

const ReservedPathSegments = new Set([
  '_next',
  'account-holder',
  'admin',
  'api',
  'courses',
  'educators',
  'env.js',
  'favicon.ico',
  'login',
  'members',
  'rooms',
  'semesters',
  'settings',
  'students',
  'unauthorized',
]);
