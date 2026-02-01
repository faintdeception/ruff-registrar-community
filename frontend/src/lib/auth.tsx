import React, { createContext, useContext, useEffect, useState } from 'react';
import { useRouter } from 'next/router';

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
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<string | null>;
  isLoading: boolean;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

const runtimeEnv = typeof window !== 'undefined' ? (window as any).__ENV__ : undefined;

const KEYCLOAK_URL = runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_URL || process.env.NEXT_PUBLIC_KEYCLOAK_URL || 'http://localhost:8080';
const KEYCLOAK_REALM = runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_REALM || process.env.NEXT_PUBLIC_KEYCLOAK_REALM || 'student-registrar';
const KEYCLOAK_CLIENT_ID = runtimeEnv?.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || process.env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID || 'student-registrar';
const API_BASE_URL = runtimeEnv?.NEXT_PUBLIC_API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const router = useRouter();

  useEffect(() => {
    const token = localStorage.getItem('accessToken');
    if (token) {
      // Check if token is expired before using it
      if (isTokenExpired(token)) {
        // Try to refresh the token
        refreshToken().then((newToken) => {
          if (newToken) {
            fetchUserInfo(newToken);
          } else {
            // Refresh failed, logout
            logout();
          }
        });
      } else {
        // Token is still valid, use it
        fetchUserInfo(token);
      }
    } else {
      setIsLoading(false);
    }
  }, []);

  const isTokenExpired = (token: string): boolean => {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      // Add 5 minute buffer before actual expiration
      return payload.exp <= (currentTime + 300);
    } catch (error) {
      console.error('Error checking token expiration:', error);
      return true; // Assume expired if we can't parse it
    }
  };

  const refreshToken = async (): Promise<string | null> => {
    try {
      const storedRefreshToken = localStorage.getItem('refreshToken');
      if (!storedRefreshToken) {
        return null;
      }

      const tokenResponse = await fetch(
        `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
          },
          body: new URLSearchParams({
            grant_type: 'refresh_token',
            client_id: KEYCLOAK_CLIENT_ID,
            refresh_token: storedRefreshToken,
          }),
        }
      );

      if (!tokenResponse.ok) {
        // Refresh token is invalid/expired
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
        return null;
      }

      const tokenData = await tokenResponse.json();
      const newAccessToken = tokenData.access_token;
      const newRefreshToken = tokenData.refresh_token;

      // Store new tokens
      localStorage.setItem('accessToken', newAccessToken);
      if (newRefreshToken) {
        localStorage.setItem('refreshToken', newRefreshToken);
      }

      return newAccessToken;
    } catch (error) {
      console.error('Error refreshing token:', error);
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      return null;
    }
  };

  const fetchUserInfo = async (token: string) => {
    try {
      // Parse JWT token to extract user info
      const payload = JSON.parse(atob(token.split('.')[1]));
      
      // Extract user information from Keycloak token
      const userData = {
        id: payload.sub,
        username: payload.preferred_username,
        email: payload.email,
        firstName: payload.given_name,
        lastName: payload.family_name,
        roles: payload.realm_access?.roles || [],
      };
      
      setUser(userData);
    } catch (error) {
      console.error('Error parsing token:', error);
      localStorage.removeItem('accessToken');
      localStorage.removeItem('refreshToken');
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (username: string, password: string) => {
    try {
      const tokenResponse = await fetch(
        `${KEYCLOAK_URL}/realms/${KEYCLOAK_REALM}/protocol/openid-connect/token`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
          },
          body: new URLSearchParams({
            grant_type: 'password',
            client_id: KEYCLOAK_CLIENT_ID,
            username,
            password,
          }),
        }
      );

      if (!tokenResponse.ok) {
        const error = await tokenResponse.json();
        throw new Error(error.error_description || 'Login failed');
      }

      const tokenData = await tokenResponse.json();
      const accessToken = tokenData.access_token;
      const refreshTokenValue = tokenData.refresh_token;

      // Store tokens
      localStorage.setItem('accessToken', accessToken);
      if (refreshTokenValue) {
        localStorage.setItem('refreshToken', refreshTokenValue);
      }

      // Get user info
      await fetchUserInfo(accessToken);

      // Redirect to dashboard
      router.push('/');
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  };

  const logout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    setUser(null);
    router.push('/login');
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
