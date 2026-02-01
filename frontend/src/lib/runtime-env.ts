export type RuntimeEnv = {
  NEXT_PUBLIC_KEYCLOAK_URL?: string;
  NEXT_PUBLIC_KEYCLOAK_REALM?: string;
  NEXT_PUBLIC_KEYCLOAK_CLIENT_ID?: string;
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

export const getKeycloakConfig = () => {
  const runtimeEnv = getRuntimeEnv();

  return {
    url: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_URL || process.env.NEXT_PUBLIC_KEYCLOAK_URL || 'http://localhost:8080',
    realm: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_REALM || process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'student-registrar',
    clientId: runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'student-registrar',
  };
};
