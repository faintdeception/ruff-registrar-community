import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { buildTenantPath, buildTenantRequestHeaders, getApiBaseUrl } from './runtime-env';
import { CSRF_HEADER_NAME, getCsrfToken, isUnsafeHttpMethod } from './csrf';

interface RetryableAxiosRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean;
}

const API_BASE_URL = getApiBaseUrl();

export const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  withCredentials: true,
  xsrfCookieName: 'studentregistrar.csrf',
  xsrfHeaderName: CSRF_HEADER_NAME,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use(
  (config) => {
    Object.assign(config.headers, buildTenantRequestHeaders());

    if (isUnsafeHttpMethod(config.method)) {
      const csrfToken = getCsrfToken();
      if (csrfToken) {
        config.headers[CSRF_HEADER_NAME] = csrfToken;
      }
    }

    return config;
  },
  (error) => Promise.reject(error)
);

// Add response interceptor to handle auth errors and token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableAxiosRequestConfig | undefined;

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      originalRequest._retry = true;
      window.location.href = buildTenantPath('/login');
    }

    return Promise.reject(error);
  }
);

export default api;
