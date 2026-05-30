import { buildTenantPath } from './tenant-routing';
import { getApiBaseUrl, getTenantSlugFromPath } from './runtime-env';
import { getAccessToken, getCurrentAccessToken } from './auth';

interface ApiClientOptions extends RequestInit {
  headers?: Record<string, string>;
}

interface TenantBlockedPayload {
  error?: string;
  code?: string;
  tenant?: string;
  tenantStatus?: string;
  canRecoverBilling?: boolean;
}

class ApiClient {
  private refreshPromise: Promise<string | null> | null = null;
  
  async request(url: string, options: ApiClientOptions = {}): Promise<Response> {
    const requestUrl = resolveApiUrl(url);
    const token = getCurrentAccessToken() ?? await this.refreshTokenIfNeeded();
    
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers || {}),
    };

    const tenantSlug = getTenantSlugFromPath();
    if (tenantSlug && shouldForwardTenantContext(requestUrl)) {
      headers['X-Tenant-Slug'] = tenantSlug;
    }
    
    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }
    
    try {
      const response = await fetch(requestUrl, {
        ...options,
        headers,
      });
      
      if (response.status === 401 && token) {
        const newToken = await this.refreshTokenIfNeeded();
        if (newToken) {
          headers['Authorization'] = `Bearer ${newToken}`;
          return fetch(requestUrl, {
            ...options,
            headers,
          });
        } else {
          window.location.href = '/login';
          throw new Error('Authentication failed');
        }
      }

      await redirectIfTenantBlocked(response);
      
      return response;
    } catch (error) {
      throw error;
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
      return await getAccessToken();
    } catch (error) {
      console.error('Error refreshing token:', error);
      return null;
    }
  }
  
  // Convenience methods
  async get(url: string, options?: Omit<ApiClientOptions, 'method' | 'body'>) {
    const response = await this.request(url, { ...options, method: 'GET' });
    return response;
  }
  
  async post(url: string, data?: unknown, options?: Omit<ApiClientOptions, 'method' | 'body'>) {
    const response = await this.request(url, {
      ...options,
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    });
    return response;
  }
  
  async put(url: string, data?: unknown, options?: Omit<ApiClientOptions, 'method' | 'body'>) {
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

const redirectIfTenantBlocked = async (response: Response): Promise<void> => {
  if (typeof window === 'undefined' || ![403, 404].includes(response.status)) {
    return;
  }

  if (window.location.pathname.includes('/tenant-status')) {
    return;
  }

  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('application/json')) {
    return;
  }

  let payload: TenantBlockedPayload | null = null;
  try {
    payload = await response.clone().json() as TenantBlockedPayload;
  } catch {
    return;
  }

  if (!payload || (payload.code !== 'tenant_access_blocked' && payload.code !== 'organization_not_found')) {
    return;
  }

  const tenantSlug = payload.tenant || getTenantSlugFromPath();
  const target = buildTenantStatusPath(tenantSlug, payload);
  if (window.location.pathname + window.location.search === target) {
    return;
  }

  window.location.href = target;
};

const buildTenantStatusPath = (tenantSlug: string | null, payload: TenantBlockedPayload): string => {
  const searchParams = new URLSearchParams();
  if (payload.tenantStatus) {
    searchParams.set('status', payload.tenantStatus);
  }
  if (payload.error) {
    searchParams.set('message', payload.error);
  }
  if (payload.canRecoverBilling) {
    searchParams.set('recover', 'true');
  }

  const basePath = buildTenantPath('/tenant-status', tenantSlug);
  const query = searchParams.toString();
  return query ? `${basePath}?${query}` : basePath;
};

const resolveApiUrl = (url: string): string => {
  if (url.startsWith('/api/')) {
    return `${getApiBaseUrl().replace(/\/$/, '')}${url}`;
  }

  return url;
};

const shouldForwardTenantContext = (url: string): boolean => {
  if (typeof window === 'undefined') {
    return false;
  }

  try {
    return url.startsWith('/api/') || new URL(url, window.location.origin).origin !== window.location.origin;
  } catch {
    return false;
  }
};
