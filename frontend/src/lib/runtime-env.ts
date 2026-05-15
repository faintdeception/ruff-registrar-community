export type RuntimeEnv = {
  NEXT_PUBLIC_KEYCLOAK_URL?: string;
  NEXT_PUBLIC_KEYCLOAK_REALM?: string;
  NEXT_PUBLIC_KEYCLOAK_CLIENT_ID?: string;
  NEXT_PUBLIC_TENANCY_BASE_DOMAIN?: string;
  NEXT_PUBLIC_TENANCY_APP_BASE_URL?: string;
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

  return '/backend';
};

export const getForwardedHost = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  return window.location.host || null;
};

export const getTenantSubdomain = (pathname?: string | null): string | null => {
  const tenantFromPath = getTenantSubdomainFromPath(pathname);
  if (tenantFromPath) {
    return tenantFromPath;
  }

  return getTenantSubdomainFromHost();
};

export const buildTenantPath = (path: string, pathname?: string | null): string => {
  const normalizedPath = normalizePath(path);
  const tenantSubdomain = getTenantSubdomain(pathname);

  if (!tenantSubdomain) {
    return normalizedPath;
  }

  if (normalizedPath === `/${tenantSubdomain}` || normalizedPath.startsWith(`/${tenantSubdomain}/`)) {
    return normalizedPath;
  }

  return normalizedPath === '/'
    ? `/${tenantSubdomain}`
    : `/${tenantSubdomain}${normalizedPath}`;
};

export const buildTenantRequestHeaders = (pathname?: string | null): Record<string, string> => {
  const forwardedHost = getForwardedHost();
  const tenantSubdomain = getTenantSubdomain(pathname);

  return {
    ...(forwardedHost ? { 'X-Forwarded-Host': forwardedHost } : {}),
    ...(tenantSubdomain ? { 'X-Tenant-Subdomain': tenantSubdomain } : {}),
  };
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

  const baseDomain =
    runtimeEnv?.NEXT_PUBLIC_TENANCY_BASE_DOMAIN ||
    process.env.NEXT_PUBLIC_TENANCY_BASE_DOMAIN ||
    '';

  const tenantRealm = getTenantRealm(baseDomain);

  return {
    url: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_URL || process.env.NEXT_PUBLIC_KEYCLOAK_URL || 'http://localhost:8080',
    realm: tenantRealm || defaultRealm,
    clientId: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'student-registrar-spa',
  };
};

const getTenantRealm = (baseDomain: string): string | null => {
  const tenantSubdomain = getTenantSubdomain() ?? getTenantSubdomainFromHost(baseDomain);
  if (!tenantSubdomain || tenantSubdomain === 'www') {
    return null;
  }

  return `${tenantSubdomain}-org`;
};

const getTenantSubdomainFromPath = (pathname?: string | null): string | null => {
  const effectivePath = pathname ?? getCurrentPathname();
  if (!effectivePath) {
    return null;
  }

  const [firstSegment] = effectivePath.split('?')[0].split('#')[0].split('/').filter(Boolean);
  if (!firstSegment) {
    return null;
  }

  const normalizedSegment = firstSegment.toLowerCase();
  return RESERVED_PATH_SEGMENTS.has(normalizedSegment) ? null : normalizedSegment;
};

const getTenantSubdomainFromHost = (configuredBaseDomain?: string): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  const runtimeEnv = getRuntimeEnv();
  const baseDomain =
    configuredBaseDomain
    || runtimeEnv?.NEXT_PUBLIC_TENANCY_BASE_DOMAIN
    || process.env.NEXT_PUBLIC_TENANCY_BASE_DOMAIN
    || '';
  const hostname = window.location.hostname.toLowerCase();

  if (!hostname || hostname === 'localhost' || isIpAddress(hostname)) {
    return null;
  }

  const subdomain = resolveSubdomain(hostname, baseDomain);
  if (!subdomain || subdomain === 'www' || RESERVED_PATH_SEGMENTS.has(subdomain)) {
    return null;
  }

  return subdomain;
};

const resolveSubdomain = (hostname: string, baseDomain: string): string | null => {
  const normalizedBase = baseDomain.toLowerCase();
  if (!normalizedBase) {
    if (hostname.endsWith('.localhost')) {
      const prefix = hostname.slice(0, -'.localhost'.length);
      return prefix ? prefix.split('.')[0] : null;
    }

    return null;
  }

  if (hostname === normalizedBase) {
    return null;
  }

  if (hostname.endsWith(`.${normalizedBase}`)) {
    const prefix = hostname.slice(0, -(normalizedBase.length + 1));
    if (!prefix) {
      return null;
    }

    return prefix.split('.')[0];
  }

  return null;
};

const normalizePath = (path: string): string => {
  if (!path) {
    return '/';
  }

  return path.startsWith('/') ? path : `/${path}`;
};

const getCurrentPathname = (): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  return window.location.pathname || null;
};

const RESERVED_PATH_SEGMENTS = new Set([
  'account-holder',
  'admin',
  'api',
  'auth',
  'backend',
  'courses',
  'educators',
  'env.js',
  'favicon.ico',
  'login',
  'members',
  'rooms',
  'semesters',
  'settings',
  'silent-check-sso.html',
  'students',
  'unauthorized',
  '_next',
]);

const isIpAddress = (hostname: string): boolean => {
  return /^\d{1,3}(\.\d{1,3}){3}$/.test(hostname) || hostname.includes(':');
};
