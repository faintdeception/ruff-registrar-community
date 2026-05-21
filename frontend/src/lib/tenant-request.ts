import type { NextApiRequest } from 'next';

const reservedPathSegments = new Set([
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

export const getTenantSlugFromRequest = (req: NextApiRequest): string | null => {
  const explicitHeader = req.headers['x-tenant-slug'];
  if (typeof explicitHeader === 'string') {
    const normalizedHeader = explicitHeader.trim().toLowerCase();
    if (isValidTenantSlug(normalizedHeader)) {
      return normalizedHeader;
    }
  }

  const requestPath = typeof req.url === 'string' ? tryGetTenantSlugFromPath(req.url) : null;
  if (requestPath) {
    return requestPath;
  }

  const referer = req.headers.referer;
  if (typeof referer !== 'string' || referer.trim().length === 0) {
    return null;
  }

  try {
    return tryGetTenantSlugFromPath(new URL(referer).pathname);
  } catch {
    return null;
  }
};

const tryGetTenantSlugFromPath = (path: string): string | null => {
  const candidate = path
    .split('?')[0]
    .split('/')
    .map(segment => segment.trim().toLowerCase())
    .filter(Boolean)[0];

  if (!candidate || reservedPathSegments.has(candidate) || !isValidTenantSlug(candidate)) {
    return null;
  }

  return candidate;
};

const isValidTenantSlug = (value: string): boolean => {
  return /^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$/.test(value);
};
