import ProtectedRoute from '@/components/ProtectedRoute';
import { useAuth } from '@/lib/auth';

export default function ProfileSettings() {
  const { user } = useAuth();
  const isAdmin = user?.roles.includes('Administrator');
  const isEducator = user?.roles.includes('Educator');

  return (
    <ProtectedRoute>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900" data-testid="profile-settings-title">
            Profile Settings
          </h1>
          <p className="mt-2 text-gray-600">
            Manage your personal information and preferences
          </p>
        </div>

        <div className="bg-white shadow-sm rounded-lg p-6">
          <div className="text-center py-12">
            <h2 className="text-xl font-semibold text-gray-700 mb-2" data-testid="coming-soon-message">
              Coming Soon
            </h2>
            <p className="text-gray-600">
              This feature is currently under development.
            </p>
            
            {(isAdmin || isEducator) && (
              <div className="mt-6 p-4 bg-blue-50 rounded-md">
                <h3 className="text-sm font-medium text-blue-900 mb-2">
                  Available Features (Coming Soon)
                </h3>
                <ul className="text-sm text-blue-700 text-left max-w-md mx-auto">
                  <li className="mb-1">• Update basic profile information</li>
                  {(isAdmin || isEducator) && (
                    <li className="mb-1">• Edit educational biography</li>
                  )}
                  {(isAdmin || isEducator) && (
                    <li className="mb-1">• Configure accepted payment types</li>
                  )}
                </ul>
              </div>
            )}
          </div>
        </div>
      </div>
    </ProtectedRoute>
  );
}
