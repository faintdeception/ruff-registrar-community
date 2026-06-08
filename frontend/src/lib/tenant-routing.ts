const tenantPathNamespace = 'org';

export const extractTenantSlugFromPath = (path: string): string | null => {
  const segments = getPathSegments(path, true);
  if (segments.length < 2 || segments[0] !== tenantPathNamespace) {
    return null;
  }

  const candidate = segments[1];
  if (!candidate || !isValidTenantSlug(candidate)) {
    return null;
  }

  return candidate;
};

export const stripTenantSlugFromPath = (path: string): string => {
  const pathname = path.split('?')[0].split('#')[0];
  const segments = getPathSegments(pathname);

  if (segments.length === 0) {
    return '/';
  }

  const tenantSlug = extractTenantSlugFromPath(pathname);
  if (!tenantSlug) {
    return pathname || '/';
  }

  const strippedSegments = segments.slice(2);
  return strippedSegments.length === 0 ? '/' : `/${strippedSegments.join('/')}`;
};

export const buildTenantPath = (targetPath: string, tenantSlug: string | null | undefined): string => {
  const normalizedTargetPath = normalizeTargetPath(targetPath);
  if (!tenantSlug) {
    return normalizedTargetPath;
  }

  const existingSlug = extractTenantSlugFromPath(normalizedTargetPath);
  if (existingSlug) {
    return normalizedTargetPath;
  }

  if (normalizedTargetPath === '/') {
    return `/${tenantPathNamespace}/${tenantSlug}`;
  }

  return `/${tenantPathNamespace}/${tenantSlug}${normalizedTargetPath}`;
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

const getPathSegments = (path: string, normalizeCase = false): string[] => {
  return path
    .split('?')[0]
    .split('#')[0]
    .split('/')
    .map(segment => normalizeCase ? segment.trim().toLowerCase() : segment.trim())
    .filter(Boolean);
};
