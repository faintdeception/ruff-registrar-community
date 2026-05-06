import { useState } from 'react';
import { useAuth } from '@/lib/auth';
import { getAppVersion } from '@/lib/runtime-env';
import { AcademicCapIcon, ArrowRightCircleIcon } from '@heroicons/react/24/outline';

export default function Login() {
  const appVersion = getAppVersion();
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  
  const { login } = useAuth();

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
          </div>

          <div className="mt-6">
            <div className="relative">
              <div className="absolute inset-0 flex items-center">
                <div className="w-full border-t border-gray-300" />
              </div>
              <div className="relative flex justify-center text-sm">
                <span className="px-2 bg-white text-gray-500">
                  Development Login
                </span>
              </div>
            </div>

            <div className="mt-6 bg-gray-50 rounded-md p-4">
              <p className="text-xs text-gray-600 mb-2">
                <strong>Development Login:</strong>
              </p>
              <p className="text-xs text-gray-600">
                Use your Keycloak username on the redirected login page.
              </p>
              <p className="text-xs text-gray-600">
                Local test accounts created by bootstrap, like <code className="bg-gray-200 px-1 rounded">scoopadmin</code>, still work.
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="fixed bottom-2 right-3 text-xs text-gray-400">
        v{appVersion}
      </div>
    </div>
  );
}
