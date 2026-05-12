import React, { createContext, useContext, useEffect, useState } from 'react';
import { useRouter } from 'next/router';
import { getApiBaseUrl, getForwardedHost } from './runtime-env';
import { CSRF_HEADER_NAME, getCsrfToken } from './csrf';

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
let currentUser: User | null = null;

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

const getAuthUrl = (path: string): string => {
  return `${getApiBaseUrl().replace(/\/$/, '')}${path}`;
};

const buildAuthHeaders = (): Record<string, string> => {
  const forwardedHost = getForwardedHost();

  return {
    'Content-Type': 'application/json',
    ...(forwardedHost ? { 'X-Forwarded-Host': forwardedHost } : {}),
  };
};

const fetchCurrentUser = async (): Promise<User | null> => {
  let response: Response;

  try {
    response = await fetch(getAuthUrl('/auth/me'), {
      credentials: 'include',
      headers: buildAuthHeaders(),
    });
  } catch (error) {
    console.warn('Session bootstrap failed; treating as signed out.', error);
    return null;
  }

  if (response.status === 401) {
    return null;
  }

  if (!response.ok) {
    console.warn(`Session bootstrap returned ${response.status}; treating as signed out.`);
    return null;
  }

  const user = await response.json() as User;
  return user;
};

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    let cancelled = false;

    const initializeAuth = async () => {
      try {
        const nextUser = await fetchCurrentUser();
        if (cancelled) {
          return;
        }

        currentUser = nextUser;
        setUser(nextUser);

        if (nextUser && router.pathname === '/login') {
          void router.replace('/');
        }
      } catch (error) {
        console.error('Error initializing session authentication:', error);
        if (!cancelled) {
          currentUser = null;
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
      const nextUser = await fetchCurrentUser();
      currentUser = nextUser;
      setUser(nextUser);
      return null;
    } catch (error) {
      console.error('Error refreshing auth state:', error);
      currentUser = null;
      setUser(null);
      return null;
    }
  };

  const login = async (username?: string, password?: string) => {
    if (!username || !password) {
      throw new Error('Email and password are required');
    }

    const response = await fetch(getAuthUrl('/auth/login'), {
      method: 'POST',
      credentials: 'include',
      headers: buildAuthHeaders(),
      body: JSON.stringify({
        email: username,
        password,
      }),
    });

    const payload = await response.json().catch(() => null) as { success?: boolean; errorMessage?: string; user?: User } | null;

    if (!response.ok || !payload?.success || !payload.user) {
      throw new Error(payload?.errorMessage ?? 'Unable to sign in');
    }

    currentUser = payload.user;
    setUser(payload.user);
    await router.replace('/');
  };

  const logout = () => {
    currentUser = null;
    setUser(null);
    void fetch(getAuthUrl('/auth/logout'), {
      method: 'POST',
      credentials: 'include',
      headers: {
        ...buildAuthHeaders(),
        [CSRF_HEADER_NAME]: getCsrfToken() ?? '',
      },
    }).finally(() => {
      void router.push('/login');
    });
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
  return null;
};

export const getCurrentAccessToken = (): string | null => {
  return null;
};

export const isAuthenticated = (): boolean => {
  return Boolean(currentUser);
};
