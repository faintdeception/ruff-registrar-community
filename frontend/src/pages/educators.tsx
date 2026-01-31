import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/router';
import { useAuth } from '@/lib/auth';
import { getApiBaseUrl } from '@/lib/runtime-env';
import ProtectedRoute from '@/components/ProtectedRoute';
import { EducatorDto, CreateEducatorDto } from '@/types';

const EducatorsPage = () => {
  const { user } = useAuth();
  const apiBaseUrl = getApiBaseUrl();
  const [educators, setEducators] = useState<EducatorDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const router = useRouter();

  const [newEducator, setNewEducator] = useState<CreateEducatorDto>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    isActive: true,
    educatorInfo: {
      bio: '',
      qualifications: [],
      specializations: [],
      department: '',
      customFields: {}
    }
  });

  const isAdmin = user?.roles?.includes('Administrator');

  useEffect(() => {
    fetchEducators();
  }, []);

  const fetchEducators = async () => {
    try {
      const accessToken = localStorage.getItem('accessToken');
      if (!accessToken) {
        router.push('/login');
        return;
      }

      const response = await fetch(`${apiBaseUrl}/api/Educators`, {
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setEducators(data);
      } else {
        setError('Failed to load educators');
      }
    } catch (err) {
      setError('Error loading educators');
      console.error('Error loading educators:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleAddEducator = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      const accessToken = localStorage.getItem('accessToken');
      if (!accessToken) {
        router.push('/login');
        return;
      }

      const response = await fetch(`${apiBaseUrl}/api/Educators`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(newEducator)
      });

      if (response.ok) {
        const created = await response.json();
        setEducators([...educators, created]);
        setShowAddForm(false);
        setNewEducator({
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          isActive: true,
          educatorInfo: {
            bio: '',
            qualifications: [],
            specializations: [],
            department: '',
            customFields: {}
          }
        });
      } else {
        setError('Failed to create educator');
      }
    } catch (err) {
      setError('Error creating educator');
      console.error('Error creating educator:', err);
    }
  };

  const handleToggleActive = async (id: string, currentStatus: boolean) => {
    if (!isAdmin) return;
    
    try {
      const accessToken = localStorage.getItem('accessToken');
      if (!accessToken) {
        router.push('/login');
        return;
      }

      const endpoint = currentStatus ? 'deactivate' : 'activate';
      const response = await fetch(`${apiBaseUrl}/api/Educators/${id}/${endpoint}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${accessToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (response.ok) {
        // Refresh the educators list
        fetchEducators();
      } else {
        setError(`Failed to ${endpoint} educator`);
      }
    } catch (err) {
      setError(`Error ${currentStatus ? 'deactivating' : 'activating'} educator`);
      console.error(`Error ${currentStatus ? 'deactivating' : 'activating'} educator:`, err);
    }
  };

  if (isLoading) {
    return (
      <ProtectedRoute>
        <div className="flex justify-center items-center h-64">
          <div className="text-lg">Loading educators...</div>
        </div>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute>
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-3xl font-bold text-gray-900">Educators</h1>
          {isAdmin && (
            <button
              onClick={() => setShowAddForm(true)}
              className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded"
            >
              Add Educator
            </button>
          )}
        </div>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
            {error}
          </div>
        )}

        {/* Add Educator Form */}
        {showAddForm && (
          <div className="mb-8 p-6 bg-gray-50 rounded-lg">
            <h2 className="text-xl font-semibold mb-4">Add New Educator</h2>
            <form onSubmit={handleAddEducator} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    First Name *
                  </label>
                  <input
                    type="text"
                    value={newEducator.firstName}
                    onChange={(e) => setNewEducator({...newEducator, firstName: e.target.value})}
                    required
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Last Name *
                  </label>
                  <input
                    type="text"
                    value={newEducator.lastName}
                    onChange={(e) => setNewEducator({...newEducator, lastName: e.target.value})}
                    required
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Email
                  </label>
                  <input
                    type="email"
                    value={newEducator.email}
                    onChange={(e) => setNewEducator({...newEducator, email: e.target.value})}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Phone
                  </label>
                  <input
                    type="tel"
                    value={newEducator.phone}
                    onChange={(e) => setNewEducator({...newEducator, phone: e.target.value})}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Department
                  </label>
                  <input
                    type="text"
                    value={newEducator.educatorInfo?.department || ''}
                    onChange={(e) => setNewEducator({
                      ...newEducator, 
                      educatorInfo: {
                        ...newEducator.educatorInfo!,
                        department: e.target.value
                      }
                    })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div className="flex items-center">
                  <input
                    type="checkbox"
                    id="isActive"
                    checked={newEducator.isActive}
                    onChange={(e) => setNewEducator({...newEducator, isActive: e.target.checked})}
                    className="mr-2"
                  />
                  <label htmlFor="isActive" className="text-sm font-medium text-gray-700">
                    Active Educator
                  </label>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Bio
                </label>
                <textarea
                  value={newEducator.educatorInfo?.bio || ''}
                  onChange={(e) => setNewEducator({
                    ...newEducator, 
                    educatorInfo: {
                      ...newEducator.educatorInfo!,
                      bio: e.target.value
                    }
                  })}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={() => setShowAddForm(false)}
                  className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600"
                >
                  Create Educator
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Educators List */}
        <div className="bg-white shadow overflow-hidden sm:rounded-md">
          <ul className="divide-y divide-gray-200">
            {educators.map((educator) => (
              <li key={educator.id}>
                <div className="px-4 py-4 sm:px-6">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <div>
                        <h3 className="text-lg font-medium text-gray-900">
                          {educator.fullName}
                          {!educator.isActive && (
                            <span className="ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
                              Inactive
                            </span>
                          )}
                        </h3>
                        <div className="mt-1 text-sm text-gray-500">
                          {educator.email && <div>Email: {educator.email}</div>}
                          {educator.phone && <div>Phone: {educator.phone}</div>}
                          {educator.educatorInfo?.department && (
                            <div>Department: {educator.educatorInfo.department}</div>
                          )}
                          <div>Course Status: {educator.isAssignedToCourse ? `Assigned to ${educator.course?.name || 'Course'}` : 'Unassigned'}</div>
                        </div>
                      </div>
                    </div>
                    {isAdmin && (
                      <div className="flex space-x-2">
                        {educator.isActive ? (
                          <button
                            onClick={() => handleToggleActive(educator.id, true)}
                            className="text-red-600 hover:text-red-900 text-sm font-medium"
                          >
                            Deactivate
                          </button>
                        ) : (
                          <button
                            onClick={() => handleToggleActive(educator.id, false)}
                            className="text-green-600 hover:text-green-900 text-sm font-medium"
                          >
                            Activate
                          </button>
                        )}
                      </div>
                    )}
                  </div>
                  {educator.educatorInfo?.bio && (
                    <div className="mt-2 text-sm text-gray-600">
                      {educator.educatorInfo.bio}
                    </div>
                  )}
                  {educator.educatorInfo?.qualifications && educator.educatorInfo.qualifications.length > 0 && (
                    <div className="mt-2">
                      <span className="text-sm font-medium text-gray-700">Qualifications: </span>
                      <span className="text-sm text-gray-600">
                        {educator.educatorInfo.qualifications.join(', ')}
                      </span>
                    </div>
                  )}
                  {educator.educatorInfo?.specializations && educator.educatorInfo.specializations.length > 0 && (
                    <div className="mt-1">
                      <span className="text-sm font-medium text-gray-700">Specializations: </span>
                      <span className="text-sm text-gray-600">
                        {educator.educatorInfo.specializations.join(', ')}
                      </span>
                    </div>
                  )}
                </div>
              </li>
            ))}
          </ul>
          {educators.length === 0 && (
            <div className="px-4 py-8 text-center text-gray-500">
              No educators found. {isAdmin && 'Click "Add Educator" to create one.'}
            </div>
          )}
        </div>
      </main>
    </ProtectedRoute>
  );
};

export default EducatorsPage;
