import Keycloak from 'keycloak-js';
import React, { createContext, useContext, useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import { getKeycloakConfig } from './runtime-env';

interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

interface AuthContextType {
  user: User | null;
  login: (username?: string, password?: string) => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<string | null>;
  isLoading: boolean;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

type KeycloakTokenClaims = {
  sub?: string;
  preferred_username?: string;
  email?: string;
  given_name?: string;
  family_name?: string;
  realm_access?: {
    roles?: string[];
  };
};

let keycloakClient: Keycloak | null = null;
let keycloakInitPromise: Promise<boolean> | null = null;

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

const getKeycloak = () => getKeycloakConfig();

const getRedirectUri = (path = '/'): string => {
  if (typeof window === 'undefined') {
    return path;
  }

  return new URL(path, window.location.origin).toString();
};

const getKeycloakClient = (): Keycloak => {
  if (!keycloakClient) {
    const config = getKeycloak();
    keycloakClient = new Keycloak({
      url: config.url,
      realm: config.realm,
      clientId: config.clientId,
    });
  }

  return keycloakClient;
};

const mapUserFromToken = (client: Keycloak): User | null => {
  const claims = client.tokenParsed as KeycloakTokenClaims | undefined;
  if (!claims?.sub) {
    return null;
  }

  return {
    id: claims.sub,
    username: claims.preferred_username ?? '',
    email: claims.email ?? '',
    firstName: claims.given_name ?? '',
    lastName: claims.family_name ?? '',
    roles: claims.realm_access?.roles ?? [],
  };
};

const ensureKeycloakInitialized = async (): Promise<Keycloak> => {
  const client = getKeycloakClient();
  if (!keycloakInitPromise) {
    keycloakInitPromise = client.init({
      onLoad: 'check-sso',
      pkceMethod: 'S256',
      checkLoginIframe: false,
      silentCheckSsoRedirectUri: getRedirectUri('/silent-check-sso.html'),
    });
  }

  await keycloakInitPromise;
  return client;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    let cancelled = false;

    const initializeAuth = async () => {
      try {
        const client = await ensureKeycloakInitialized();
        if (cancelled) {
          return;
        }

        setUser(mapUserFromToken(client));

        client.onAuthSuccess = () => {
          setUser(mapUserFromToken(client));
          if (router.pathname === '/login') {
            void router.replace('/');
          }
        };

        client.onAuthLogout = () => {
          setUser(null);
        };

        client.onAuthRefreshSuccess = () => {
          setUser(mapUserFromToken(client));
        };

        client.onTokenExpired = () => {
          void client.updateToken(300)
            .then(() => {
              setUser(mapUserFromToken(client));
            })
            .catch(() => {
              setUser(null);
            });
        };
      } catch (error) {
        console.error('Error initializing Keycloak authentication:', error);
        if (!cancelled) {
          setUser(null);
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    };

    void initializeAuth();

    return () => {
      cancelled = true;
    };
  }, [router]);

  const refreshToken = async (): Promise<string | null> => {
    try {
      const client = await ensureKeycloakInitialized();
      if (!client.authenticated) {
        return null;
      }

      const refreshed = await client.updateToken(300);
      if (!refreshed && !client.token) {
        return null;
      }

      setUser(mapUserFromToken(client));
      return client.token ?? null;
    } catch (error) {
      console.error('Error refreshing token:', error);
      setUser(null);
      return null;
    }
  };

  const login = async () => {
    const client = await ensureKeycloakInitialized();
    await client.login({
      redirectUri: getRedirectUri('/'),
      scope: 'openid profile email',
    });
  };

  const logout = () => {
    const client = keycloakClient;
    setUser(null);
    if (client) {
      void client.logout({ redirectUri: getRedirectUri('/login') });
      return;
    }

    void router.push('/login');
  };

  const value = {
    user,
    login,
    logout,
    refreshToken,
    isLoading,
    isAuthenticated: !!user,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const getAccessToken = async (): Promise<string | null> => {
  try {
    const client = await ensureKeycloakInitialized();
    if (!client.authenticated) {
      return null;
    }

    await client.updateToken(300);
    return client.token ?? null;
  } catch {
    return null;
  }
};

export const getCurrentAccessToken = (): string | null => {
  return keycloakClient?.authenticated ? keycloakClient.token ?? null : null;
};

export const isAuthenticated = (): boolean => {
  return Boolean(keycloakClient?.authenticated && keycloakClient.token);
};
