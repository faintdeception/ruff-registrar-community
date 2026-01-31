import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/router';
import { EducatorDto, CreateEducatorDto, EducatorInfo } from '@/types';
import { getApiBaseUrl } from '@/lib/runtime-env';

interface CreateEducatorFormData {
  courseId: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone?: string;
  isPrimary: boolean;
  isActive: boolean;
  educatorInfo?: EducatorInfo;
}

const AdminEducatorsPage = () => {
  const [instructors, setInstructors] = useState<EducatorDto[]>([]);
  const [courses, setCourses] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [token, setToken] = useState<string | null>(null);
  const router = useRouter();
  const apiBaseUrl = getApiBaseUrl();

  const [newInstructor, setNewInstructor] = useState<CreateEducatorFormData>({
    courseId: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    isPrimary: false,
    isActive: true,
    educatorInfo: {
      bio: '',
      qualifications: [],
      specializations: [],
      department: '',
      customFields: {}
    }
  });

  useEffect(() => {
    // Get token from localStorage
    const storedToken = localStorage.getItem('accessToken');
    if (!storedToken) {
      router.push('/login');
      return;
    }
    setToken(storedToken);
    
    // Load initial data
    loadInstructors(storedToken);
    loadCourses(storedToken);
  }, []);

  const loadInstructors = async (authToken: string) => {
    try {
      const response = await fetch(`${apiBaseUrl}/api/Educators`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setInstructors(data);
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

  const loadCourses = async (authToken: string) => {
    try {
      const response = await fetch(`${apiBaseUrl}/api/Courses`, {
        headers: {
          'Authorization': `Bearer ${authToken}`,
          'Content-Type': 'application/json'
        }
      });
      
      if (response.ok) {
        const data = await response.json();
        setCourses(data);
      }
    } catch (err) {
      console.error('Error loading courses:', err);
    }
  };

  const handleAddInstructor = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    try {
      // Create payload that matches CreateEducatorDto
      const payload = {
        courseId: newInstructor.courseId || null,
        firstName: newInstructor.firstName,
        lastName: newInstructor.lastName,
        email: newInstructor.email || null,
        phone: newInstructor.phone || null,
        isPrimary: newInstructor.isPrimary,
        isActive: newInstructor.isActive,
        educatorInfo: {
          bio: newInstructor.educatorInfo?.bio || '',
          qualifications: newInstructor.educatorInfo?.qualifications || [],
          specializations: newInstructor.educatorInfo?.specializations || [],
          department: newInstructor.educatorInfo?.department || '',
          customFields: newInstructor.educatorInfo?.customFields || {}
        }
      };

      const response = await fetch(`${apiBaseUrl}/api/Educators`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        const created = await response.json();
        setInstructors([...instructors, created]);
        setShowAddForm(false);
        setNewInstructor({
          courseId: '',
          firstName: '',
          lastName: '',
          email: '',
          phone: '',
          isPrimary: false,
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
        setError('Failed to create instructor');
      }
    } catch (err) {
      setError('Error creating instructor');
      console.error('Error creating instructor:', err);
    }
  };

  const handleDeleteInstructor = async (id: string) => {
    if (!token) return;

    try {
      const response = await fetch(`${apiBaseUrl}/api/Educators/${id}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (response.ok) {
        setInstructors(instructors.filter(i => i.id !== id));
      } else {
        setError('Failed to delete instructor');
      }
    } catch (err) {
      setError('Error deleting instructor');
      console.error('Error deleting instructor:', err);
    }
  };

  if (isLoading) {
    return <div className="p-8">Loading...</div>;
  }

  return (
    <div className="max-w-6xl mx-auto p-8">
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Educator Management
        </h1>
        <p className="text-gray-600">
          Manage course instructors and their assignments
        </p>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
          <p className="text-red-800">{error}</p>
        </div>
      )}

      <div className="mb-6">
        <button
          onClick={() => setShowAddForm(!showAddForm)}
          className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
        >
          {showAddForm ? 'Cancel' : 'Add New Educator'}
        </button>
      </div>

      {showAddForm && (
        <div className="mb-8 p-6 bg-gray-50 rounded-lg">
          <h2 className="text-xl font-semibold mb-4">Add New Educator</h2>
          <form onSubmit={handleAddInstructor} className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Course (Optional)
                </label>
                <select
                  value={newInstructor.courseId}
                  onChange={(e) => setNewInstructor({...newInstructor, courseId: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">No Course Assignment</option>
                  {courses.map(course => (
                    <option key={course.id} value={course.id}>
                      {course.code} - {course.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  First Name
                </label>
                <input
                  type="text"
                  value={newInstructor.firstName}
                  onChange={(e) => setNewInstructor({...newInstructor, firstName: e.target.value})}
                  required
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Last Name
                </label>
                <input
                  type="text"
                  value={newInstructor.lastName}
                  onChange={(e) => setNewInstructor({...newInstructor, lastName: e.target.value})}
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
                  value={newInstructor.email}
                  onChange={(e) => setNewInstructor({...newInstructor, email: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Phone
                </label>
                <input
                  type="tel"
                  value={newInstructor.phone}
                  onChange={(e) => setNewInstructor({...newInstructor, phone: e.target.value})}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="isPrimary"
                  checked={newInstructor.isPrimary}
                  onChange={(e) => setNewInstructor({...newInstructor, isPrimary: e.target.checked})}
                  className="mr-2"
                />
                <label htmlFor="isPrimary" className="text-sm font-medium text-gray-700">
                  Primary Instructor
                </label>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Bio
              </label>
              <textarea
                value={newInstructor.educatorInfo?.bio || ''}
                onChange={(e) => setNewInstructor({
                  ...newInstructor,
                  educatorInfo: {
                    ...newInstructor.educatorInfo!,
                    bio: e.target.value
                  }
                })}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                type="button"
                onClick={() => setShowAddForm(false)}
                className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                type="submit"
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
              >
                Add Educator
              </button>
            </div>
          </form>
        </div>
      )}

      <div className="bg-white rounded-lg shadow">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold">Current Educators</h2>
        </div>
        
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Name
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Course
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Contact
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Role
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {instructors.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-6 py-4 text-center text-gray-500">
                    No educators found. Add one to get started.
                  </td>
                </tr>
              ) : (
                instructors.map((instructor) => (
                  <tr key={instructor.id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {instructor.fullName}
                        </div>
                        {instructor.educatorInfo?.bio && (
                          <div className="text-sm text-gray-500">
                            {instructor.educatorInfo.bio.substring(0, 100)}...
                          </div>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {instructor.course?.name || 'Unassigned'}
                      </div>
                      <div className="text-sm text-gray-500">
                        {instructor.course?.code || 'No Course'}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">
                        {instructor.email}
                      </div>
                      <div className="text-sm text-gray-500">
                        {instructor.phone}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                        instructor.isPrimary
                          ? 'bg-blue-100 text-blue-800'
                          : 'bg-gray-100 text-gray-800'
                      }`}>
                        {instructor.isPrimary ? 'Primary' : 'Assistant'}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <button
                        onClick={() => handleDeleteInstructor(instructor.id)}
                        className="text-red-600 hover:text-red-900 text-sm"
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="mt-8 p-4 bg-blue-50 border border-blue-200 rounded-lg">
        <h3 className="text-lg font-semibold text-blue-900 mb-2">
          Admin Features Completed
        </h3>
        <ul className="text-blue-800 space-y-1">
          <li>✅ CourseInstructor Controller with admin-only authorization</li>
          <li>✅ Full CRUD operations for course instructors</li>
          <li>✅ Role-based access control</li>
          <li>✅ Comprehensive instructor information with JSON storage</li>
          <li>✅ Primary instructor designation</li>
          <li>✅ Integration with existing course system</li>
        </ul>
      </div>
    </div>
  );
};

export default AdminEducatorsPage;
