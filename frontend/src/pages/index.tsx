import Link from 'next/link';
import { useState, useEffect } from 'react';
import { useAuth } from '@/lib/auth';
import ProtectedRoute from '@/components/ProtectedRoute';
import api from '@/lib/api';
import { 
  AcademicCapIcon, 
  UserGroupIcon, 
  BookOpenIcon, 
  ClipboardDocumentListIcon,
  ChartBarIcon,
  ArrowRightOnRectangleIcon,
  UserCircleIcon
} from '@heroicons/react/24/outline';

export default function Home() {
  const { user } = useAuth();
  const [stats, setStats] = useState({
    totalStudents: 0,
    totalCourses: 0,
    totalEducators: 0,
    totalRooms: 0,
  });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setLoading(true);
        setError(null);

        // Fetch data from all endpoints in parallel
        const [studentsResponse, coursesResponse, educatorsResponse, roomsResponse] = await Promise.allSettled([
          api.get('/students'),
          api.get('/courses'),
          api.get('/educators'),
          api.get('/rooms')
        ]);

        // Extract counts from successful responses
        const totalStudents = studentsResponse.status === 'fulfilled' ? studentsResponse.value.data.length : 0;
        const totalCourses = coursesResponse.status === 'fulfilled' ? coursesResponse.value.data.length : 0;
        const totalEducators = educatorsResponse.status === 'fulfilled' ? educatorsResponse.value.data.length : 0;
        const totalRooms = roomsResponse.status === 'fulfilled' ? roomsResponse.value.data.length : 0;

        setStats({
          totalStudents,
          totalCourses,
          totalEducators,
          totalRooms,
        });

        // Log any failed requests for debugging
        const failures = [studentsResponse, coursesResponse, educatorsResponse, roomsResponse]
          .map((response, index) => ({ response, endpoint: ['students', 'courses', 'educators', 'rooms'][index] }))
          .filter(({ response }) => response.status === 'rejected');
        
        if (failures.length > 0) {
          console.warn('Some dashboard stats failed to load:', failures.map(f => f.endpoint));
        }

      } catch (err) {
        console.error('Error fetching dashboard stats:', err);
        setError('Failed to load dashboard statistics');
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, []);

  return (
    <ProtectedRoute>
      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12">
        {/* Hero Section */}
        <div className="text-center mb-12">
          <h2 className="text-4xl font-bold text-gray-900 mb-4">
            Welcome to Student Registrar
          </h2>
          <p className="text-xl text-gray-600 max-w-2xl mx-auto">
            A comprehensive homeschool management system designed to help you 
            track students, courses, rooms, and educators with ease.
          </p>
        </div>

        {/* Stats Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-12">
          {error && (
            <div className="col-span-full bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-md">
              <p className="text-sm">{error}</p>
            </div>
          )}
          
          <div className="card">
            <div className="card-body">
              <div className="flex items-center">
                <UserGroupIcon className="h-8 w-8 text-primary-600" />
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">Total Students</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {loading ? (
                      <span className="animate-pulse bg-gray-200 rounded w-8 h-8 inline-block"></span>
                    ) : (
                      stats.totalStudents
                    )}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-body">
              <div className="flex items-center">
                <BookOpenIcon className="h-8 w-8 text-primary-600" />
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">Total Courses</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {loading ? (
                      <span className="animate-pulse bg-gray-200 rounded w-8 h-8 inline-block"></span>
                    ) : (
                      stats.totalCourses
                    )}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-body">
              <div className="flex items-center">
                <ClipboardDocumentListIcon className="h-8 w-8 text-primary-600" />
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">Educators</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {loading ? (
                      <span className="animate-pulse bg-gray-200 rounded w-8 h-8 inline-block"></span>
                    ) : (
                      stats.totalEducators
                    )}
                  </p>
                </div>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-body">
              <div className="flex items-center">
                <ChartBarIcon className="h-8 w-8 text-primary-600" />
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">Rooms</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {loading ? (
                      <span className="animate-pulse bg-gray-200 rounded w-8 h-8 inline-block"></span>
                    ) : (
                      stats.totalRooms
                    )}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <Link href="/account-holder" className="group">
            <div className="card group-hover:shadow-lg transition-shadow">
              <div className="card-body text-center">
                <UserGroupIcon className="h-12 w-12 text-primary-600 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">Account Details</h3>
                <p className="text-sm text-gray-600">View and manage your family</p>
              </div>
            </div>
          </Link>

          <Link href="/courses" className="group">
            <div className="card group-hover:shadow-lg transition-shadow">
              <div className="card-body text-center">
                <BookOpenIcon className="h-12 w-12 text-primary-600 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">Browse Courses</h3>
                <p className="text-sm text-gray-600">View available courses</p>
              </div>
            </div>
          </Link>

          <Link href="/Educators" className="group">
            <div className="card group-hover:shadow-lg transition-shadow">
              <div className="card-body text-center">
                <ClipboardDocumentListIcon className="h-12 w-12 text-primary-600 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">Educators</h3>
                <p className="text-sm text-gray-600">Manage course educators</p>
              </div>
            </div>
          </Link>

          {user?.roles?.includes('Administrator') && (
            <Link href="/members" className="group" data-testid="members-card">
              <div className="card group-hover:shadow-lg transition-shadow">
                <div className="card-body text-center">
                  <UserCircleIcon className="h-12 w-12 text-primary-600 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">Members</h3>
                  <p className="text-sm text-gray-600">Manage member accounts</p>
                </div>
              </div>
            </Link>
          )}

          <Link href="/rooms" className="group">
            <div className="card group-hover:shadow-lg transition-shadow">
              <div className="card-body text-center">
                <ChartBarIcon className="h-12 w-12 text-primary-600 mx-auto mb-4" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">Rooms</h3>
                <p className="text-sm text-gray-600">View and manage rooms</p>
              </div>
            </div>
          </Link>
        </div>
      </main>
    </ProtectedRoute>
  );
}
