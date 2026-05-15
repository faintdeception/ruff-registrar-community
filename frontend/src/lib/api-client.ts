import { buildTenantPath, buildTenantRequestHeaders, getApiBaseUrl } from './runtime-env';
import { CSRF_HEADER_NAME, getCsrfToken, isUnsafeHttpMethod } from './csrf';

interface ApiClientOptions extends RequestInit {
  headers?: Record<string, string>;
}

class ApiClient {
  async request(url: string, options: ApiClientOptions = {}): Promise<Response> {
    const requestUrl = resolveApiUrl(url);
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...buildTenantRequestHeaders(),
      ...(options.headers || {}),
    };

    if (isUnsafeHttpMethod(options.method)) {
      const csrfToken = getCsrfToken();
      if (csrfToken) {
        headers[CSRF_HEADER_NAME] = csrfToken;
      }
    }

    try {
      const response = await fetch(requestUrl, {
        ...options,
        credentials: 'include',
        headers,
      });

      if (response.status === 401) {
        window.location.href = buildTenantPath('/login');
      }

      return response;
    } catch (error) {
      throw error;
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

const resolveApiUrl = (url: string): string => {
  if (url.startsWith('/api/')) {
    return `${getApiBaseUrl().replace(/\/$/, '')}${url}`;
  }

  return url;
};
