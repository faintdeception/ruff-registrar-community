import type { NextApiRequest } from 'next';
import { extractTenantSlugFromPath, isValidTenantSlug } from './tenant-routing';

export const getTenantSlugFromRequest = (req: NextApiRequest): string | null => {
  const explicitHeader = req.headers['x-tenant-slug'];
  if (typeof explicitHeader === 'string') {
    const normalizedHeader = explicitHeader.trim().toLowerCase();
    if (isValidTenantSlug(normalizedHeader)) {
      return normalizedHeader;
    }
  }

  const requestPath = typeof req.url === 'string' ? extractTenantSlugFromPath(req.url) : null;
  if (requestPath) {
    return requestPath;
  }

  const referer = req.headers.referer;
  if (typeof referer !== 'string' || referer.trim().length === 0) {
    return null;
  }

  try {
    return extractTenantSlugFromPath(new URL(referer).pathname);
  } catch {
    return null;
  }
};

export const withTenantSlugHeader = (
  req: NextApiRequest,
  headers: Record<string, string>
): Record<string, string> => {
  const tenantSlug = getTenantSlugFromRequest(req);
  if (!tenantSlug) {
    return headers;
  }

  return {
    ...headers,
    'X-Tenant-Slug': tenantSlug,
  };
};
