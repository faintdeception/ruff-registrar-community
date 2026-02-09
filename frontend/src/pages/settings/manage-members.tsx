import ProtectedRoute from '@/components/ProtectedRoute';

export default function ManageMembersSettings() {
  return (
    <ProtectedRoute roles={['Administrator']}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900" data-testid="manage-members-title">
            Manage Members
          </h1>
          <p className="mt-2 text-gray-600">
            View and manage all system members, educators, and administrators
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
            
            <div className="mt-6 p-4 bg-blue-50 rounded-md">
              <h3 className="text-sm font-medium text-blue-900 mb-2">
                Available Features (Coming Soon)
              </h3>
              <ul className="text-sm text-blue-700 text-left max-w-md mx-auto">
                <li className="mb-1">• View all members (educators, admins)</li>
                <li className="mb-1">• Edit member details</li>
                <li className="mb-1">• Assign and manage roles</li>
                <li className="mb-1">• Deactivate or remove members</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </ProtectedRoute>
  );
}
