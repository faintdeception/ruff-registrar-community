import { useEffect, useState } from 'react';
import { useAuth } from '@/lib/auth';
import { getApiBaseUrl, getAppVersion, getTenantSlugFromPath } from '@/lib/runtime-env';
import { AcademicCapIcon, ArrowRightCircleIcon, EnvelopeIcon } from '@heroicons/react/24/outline';

export default function Login() {
  const appVersion = getAppVersion();
  const tenantSlug = getTenantSlugFromPath();
  const apiBaseUrl = getApiBaseUrl();
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [requestAccessUrl, setRequestAccessUrl] = useState<string | null>(null);
  
  const { login } = useAuth();

  useEffect(() => {
    const fetchRequestAccessUrl = async () => {
      try {
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
        };

        if (tenantSlug) {
          headers['X-Tenant-Slug'] = tenantSlug;
        }

        const response = await fetch(`${apiBaseUrl}/api/tenant-access-request`, {
          method: 'GET',
          headers,
        });

        if (!response.ok) {
          return;
        }

        const payload = await response.json() as { adminEmail?: string };
        if (!payload.adminEmail) {
          return;
        }

        const organizationLabel = tenantSlug ?? 'Student Registrar';
        const subject = encodeURIComponent(`Access request for ${organizationLabel}`);
        const body = encodeURIComponent(
          tenantSlug
            ? `Hello,\n\nI would like to request access to the ${tenantSlug} organization in Student Registrar.\n\nThanks,\n`
            : 'Hello,\n\nI would like to request access to Student Registrar.\n\nThanks,\n'
        );

        setRequestAccessUrl(`mailto:${payload.adminEmail}?subject=${subject}&body=${body}`);
      } catch {
        setRequestAccessUrl(null);
      }
    };

    void fetchRequestAccessUrl();
  }, [apiBaseUrl, tenantSlug]);

  const handleSignIn = async () => {
    setError('');
    setIsLoading(true);

    try {
      await login();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to start sign-in');
    } finally {
      setIsLoading(false);
    }
  };

  const handleRequestAccess = () => {
    if (requestAccessUrl) {
      window.location.href = requestAccessUrl;
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-md">
        <div className="flex justify-center">
          <AcademicCapIcon className="h-12 w-12 text-primary-600" />
        </div>
        <h2 className="mt-6 text-center text-3xl font-bold text-gray-900">
          Sign in to Student Registrar
        </h2>
        <p className="mt-2 text-center text-sm text-gray-600">
          Manage your homeschool students and courses
        </p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
        <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
          <div id="login-form" className="space-y-6">
            <p className="text-sm text-gray-600">
              Sign in using the organization login page. Credentials are entered directly with Keycloak instead of this app.
            </p>
            {error && (
              <div className="bg-red-50 border border-red-200 rounded-md p-3">
                <div className="flex">
                  <div className="ml-3">
                    <h3 className="text-sm font-medium text-red-800">
                      Login Error
                    </h3>
                    <div className="mt-2 text-sm text-red-700">
                      {error}
                    </div>
                  </div>
                </div>
              </div>
            )}
            <div>
              <button
                type="button"
                onClick={handleSignIn}
                disabled={isLoading}
                className={`w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white ${
                  isLoading
                    ? 'bg-gray-400 cursor-not-allowed'
                    : 'bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500'
                }`}
              >
                {isLoading ? (
                  <div className="flex items-center">
                    <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Redirecting...
                  </div>
                ) : (
                  <span className="flex items-center gap-2">
                    <ArrowRightCircleIcon className="h-5 w-5" />
                    Sign in with Keycloak
                  </span>
                )}
              </button>
            </div>

            {requestAccessUrl && (
              <div className="text-center text-sm text-gray-600">
                <a
                  data-testid="tenant-access-request-link"
                  href={requestAccessUrl}
                  className="font-medium text-primary-600 hover:text-primary-700 underline decoration-2 underline-offset-2"
                >
                  <span className="flex items-center justify-center gap-2">
                    <EnvelopeIcon className="h-4 w-4" />
                    Or click here to request access
                  </span>
                </a>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="fixed bottom-2 right-3 text-xs text-gray-400">
        v{appVersion}
      </div>
    </div>
  );
}
