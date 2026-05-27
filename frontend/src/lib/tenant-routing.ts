export const reservedTenantPathSegments = new Set([
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
  'silent-check-sso.html',
  'students',
  'unauthorized',
]);

export const extractTenantSlugFromPath = (path: string): string | null => {
  const pathname = path.split('?')[0].split('#')[0];
  const candidate = pathname
    .split('/')
    .map(segment => segment.trim().toLowerCase())
    .filter(Boolean)[0];

  if (!candidate || reservedTenantPathSegments.has(candidate) || !isValidTenantSlug(candidate)) {
    return null;
  }

  return candidate;
};

export const stripTenantSlugFromPath = (path: string): string => {
  const pathname = path.split('?')[0].split('#')[0];
  const segments = pathname
    .split('/')
    .map(segment => segment.trim())
    .filter(Boolean);

  if (segments.length === 0) {
    return '/';
  }

  const tenantSlug = extractTenantSlugFromPath(pathname);
  if (!tenantSlug) {
    return pathname || '/';
  }

  const strippedSegments = segments.slice(1);
  return strippedSegments.length === 0 ? '/' : `/${strippedSegments.join('/')}`;
};

export const buildTenantPath = (targetPath: string, tenantSlug: string | null | undefined): string => {
  const normalizedTargetPath = normalizeTargetPath(targetPath);
  if (!tenantSlug) {
    return normalizedTargetPath;
  }

  if (extractTenantSlugFromPath(normalizedTargetPath) === tenantSlug) {
    return normalizedTargetPath;
  }

  if (normalizedTargetPath === '/') {
    return `/${tenantSlug}`;
  }

  return `/${tenantSlug}${normalizedTargetPath}`;
};

export const isValidTenantSlug = (value: string): boolean => {
  return /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/.test(value);
};

const normalizeTargetPath = (value: string): string => {
  if (!value || value === '/') {
    return '/';
  }

  return value.startsWith('/') ? value : `/${value}`;
};
