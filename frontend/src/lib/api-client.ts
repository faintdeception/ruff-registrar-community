import { getKeycloakConfig } from './runtime-env';

interface ApiClientOptions extends RequestInit {
  headers?: Record<string, string>;
}

class ApiClient {
  private refreshPromise: Promise<string | null> | null = null;
  
  async request(url: string, options: ApiClientOptions = {}): Promise<Response> {
    let token = localStorage.getItem('accessToken');
    
    // Check if token is expired
    if (token && this.isTokenExpired(token)) {
      token = await this.refreshTokenIfNeeded();
      if (!token) {
        // Redirect to login if refresh failed
        window.location.href = '/login';
        throw new Error('Authentication failed');
      }
    }
    
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers || {}),
    };
    
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    
    try {
      const response = await fetch(url, {
        ...options,
        headers,
      });
      
      // If we get a 401, try to refresh the token once
      if (response.status === 401 && token) {
        const newToken = await this.refreshTokenIfNeeded();
        if (newToken) {
          // Retry the request with the new token
          headers['Authorization'] = `Bearer ${newToken}`;
          return fetch(url, {
            ...options,
            headers,
          });
        } else {
          // Refresh failed, redirect to login
          window.location.href = '/login';
          throw new Error('Authentication failed');
        }
      }
      
      return response;
    } catch (error) {
      throw error;
    }
  }
  
  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      // Add 5 minute buffer before actual expiration
      return payload.exp <= (currentTime + 300);
    } catch (error) {
      console.error('Error checking token expiration:', error);
      return true; // Assume expired if we can't parse it
    }
  }
  
  private async refreshTokenIfNeeded(): Promise<string | null> {
    // Prevent multiple simultaneous refresh attempts
    if (this.refreshPromise) {
      return this.refreshPromise;
    }
    
    this.refreshPromise = this.performTokenRefresh();
    const result = await this.refreshPromise;
    this.refreshPromise = null;
    return result;
  }
  
  private async performTokenRefresh(): Promise<string | null> {
    try {
      const refreshToken = localStorage.getItem('refreshToken');
      if (!refreshToken) {
        return null;
      }

      const keycloakConfig = getKeycloakConfig();

      const tokenResponse = await fetch(
        `${keycloakConfig.url}/realms/${keycloakConfig.realm}/protocol/openid-connect/token`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
          },
          body: new URLSearchParams({
            grant_type: 'refresh_token',
            client_id: keycloakConfig.clientId,
            refresh_token: refreshToken,
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
  }
  
  // Convenience methods
  async get(url: string, options?: Omit<ApiClientOptions, 'method' | 'body'>) {
    const response = await this.request(url, { ...options, method: 'GET' });
    return response;
  }
  
  async post(url: string, data?: any, options?: Omit<ApiClientOptions, 'method' | 'body'>) {
    const response = await this.request(url, {
      ...options,
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
    return response;
  }
  
  async put(url: string, data?: any, options?: Omit<ApiClientOptions, 'method' | 'body'>) {
    const response = await this.request(url, {
      ...options,
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    });
    return response;
  }
  
  async delete(url: string, options?: Omit<ApiClientOptions, 'method' | 'body'>) {
    const response = await this.request(url, { ...options, method: 'DELETE' });
    return response;
  }
}

// Export a singleton instance
export const apiClient = new ApiClient();
export default apiClient;
