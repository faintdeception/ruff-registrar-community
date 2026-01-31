export type RuntimeEnv = {
  NEXT_PUBLIC_KEYCLOAK_URL?: string;
  NEXT_PUBLIC_KEYCLOAK_REALM?: string;
  NEXT_PUBLIC_KEYCLOAK_CLIENT_ID?: string;
  NEXT_PUBLIC_KEYCLOAK_CLIENT_SECRET?: string;
  NEXT_PUBLIC_API_URL?: string;
};

export const getRuntimeEnv = (): RuntimeEnv | undefined => {
  if (typeof window === 'undefined') {
    return undefined;
  }

  return (window as any).__ENV__ as RuntimeEnv | undefined;
};

export const getApiBaseUrl = (): string => {
  return getRuntimeEnv()?.NEXT_PUBLIC_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
};
