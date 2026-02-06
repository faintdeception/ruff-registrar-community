export type RuntimeEnv = {
  NEXT_PUBLIC_KEYCLOAK_URL?: string;
  NEXT_PUBLIC_KEYCLOAK_REALM?: string;
  NEXT_PUBLIC_KEYCLOAK_CLIENT_ID?: string;
  NEXT_PUBLIC_TENANCY_BASE_DOMAIN?: string;
  NEXT_PUBLIC_API_URL?: string;
  NEXT_PUBLIC_APP_VERSION?: string;
};

export const getRuntimeEnv = (): RuntimeEnv | undefined => {
  if (typeof window === 'undefined') {
    return undefined;
  }

  return (window as any).__ENV__ as RuntimeEnv | undefined;
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

  const baseDomain =
    runtimeEnv?.NEXT_PUBLIC_TENANCY_BASE_DOMAIN ||
    process.env.NEXT_PUBLIC_TENANCY_BASE_DOMAIN ||
    '';

  const tenantRealm = baseDomain ? getTenantRealmFromHost(baseDomain) : null;

  return {
    url: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_URL || process.env.NEXT_PUBLIC_KEYCLOAK_URL || 'http://localhost:8080',
    realm: tenantRealm || defaultRealm,
    clientId: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'student-registrar-spa',
  };
};

const getTenantRealmFromHost = (baseDomain: string): string | null => {
  if (typeof window === 'undefined') {
    return null;
  }

  const hostname = window.location.hostname.toLowerCase();

  if (!hostname || hostname === 'localhost' || hostname.endsWith('.localhost')) {
    return null;
  }

  if (isIpAddress(hostname)) {
    return null;
  }

  const subdomain = resolveSubdomain(hostname, baseDomain);
  if (!subdomain || subdomain === 'www') {
    return null;
  }

  return `${subdomain}-org`;
};

const resolveSubdomain = (hostname: string, baseDomain: string): string | null => {
  const normalizedBase = baseDomain.toLowerCase();
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

const isIpAddress = (hostname: string): boolean => {
  return /^\d{1,3}(\.\d{1,3}){3}$/.test(hostname) || hostname.includes(':');
};
