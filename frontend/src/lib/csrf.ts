const CSRF_COOKIE_NAME = 'studentregistrar.csrf';
export const CSRF_HEADER_NAME = 'X-CSRF-TOKEN';

export const getCsrfToken = (): string | null => {
  if (typeof document === 'undefined') {
    return null;
  }

  const cookiePrefix = `${CSRF_COOKIE_NAME}=`;
  const cookie = document.cookie
    .split(';')
    .map(part => part.trim())
    .find(part => part.startsWith(cookiePrefix));

  if (!cookie) {
    return null;
  }

  return decodeURIComponent(cookie.slice(cookiePrefix.length));
};

export const isUnsafeHttpMethod = (method?: string): boolean => {
  if (!method) {
    return false;
  }

  return !['GET', 'HEAD', 'OPTIONS'].includes(method.toUpperCase());
};
