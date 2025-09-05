import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useAuth } from '@/lib/auth';
import ProtectedRoute from '@/components/ProtectedRoute';
import apiClient from '@/lib/api-client';
import {
  ClipboardDocumentListIcon,
  UserGroupIcon,
  BookOpenIcon,
  CalendarIcon,
  AcademicCapIcon,
  CheckCircleIcon,
  XCircleIcon,
  ClockIcon
} from '@heroicons/react/24/outline';

interface Enrollment {
  id: string;
  enrollmentDate: string;
  isActive: boolean;
  studentName: string;
  studentId: string;
  courseName: string;
  courseId: string;
  semesterName: string;
  semesterId: string;
  status: string;
  completionDate?: string;
  finalGrade?: string;
}

export default function EnrollmentsPage() {
  const { user } = useAuth();
  const [enrollments, setEnrollments] = useState<Enrollment[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterStatus, setFilterStatus] = useState<string>('all');

  const isAdmin = user?.roles.includes('Administrator');

  useEffect(() => {
    fetchEnrollments();
  }, []);

  const fetchEnrollments = async () => {
    try {
      setLoading(true);

      // For now, we'll need to get enrollments through the students endpoint
      // In the future, there should be a dedicated enrollments endpoint
      const response = await apiClient.get('/api/account-holders/me/students');

      if (!response.ok) {
        throw new Error('Failed to fetch enrollments');
      }

      const students = await response.json();
      
      // Extract enrollments from all students
      const allEnrollments: Enrollment[] = [];
      students.forEach((student: any) => {
        student.enrollments?.forEach((enrollment: any) => {
          allEnrollments.push({
            ...enrollment,
            studentName: `${student.firstName} ${student.lastName}`,
            studentId: student.id
          });
        });
      });

      setEnrollments(allEnrollments);
    } catch (err) {
      setError('Failed to fetch enrollments');
      console.error('Error fetching enrollments:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getStatusIcon = (status: string, isActive: boolean) => {
    if (!isActive) {
      return <XCircleIcon className="h-5 w-5 text-red-500" />;
    }
    
    switch (status?.toLowerCase()) {
      case 'completed':
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case 'active':
      case 'enrolled':
        return <ClockIcon className="h-5 w-5 text-blue-500" />;
      default:
        return <ClockIcon className="h-5 w-5 text-gray-500" />;
    }
  };

  const getStatusColor = (status: string, isActive: boolean) => {
    if (!isActive) return 'bg-red-100 text-red-800';
    
    switch (status?.toLowerCase()) {
      case 'completed':
        return 'bg-green-100 text-green-800';
      case 'active':
      case 'enrolled':
        return 'bg-blue-100 text-blue-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const filteredEnrollments = enrollments.filter(enrollment => {
    if (filterStatus === 'all') return true;
    if (filterStatus === 'active') return enrollment.isActive;
    if (filterStatus === 'completed') return enrollment.status?.toLowerCase() === 'completed';
    if (filterStatus === 'inactive') return !enrollment.isActive;
    return true;
  });

  if (loading) {
    return (
      <ProtectedRoute>
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading enrollments...</p>
          </div>
        </main>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute>
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <ClipboardDocumentListIcon className="h-8 w-8 text-primary-600" />
              <h1 className="ml-3 text-2xl font-bold text-gray-900">Enrollments</h1>
            </div>
            <div className="flex space-x-3">
              {isAdmin && (
                <Link href="/students" className="btn btn-secondary">
                  <UserGroupIcon className="h-5 w-5" />
                  Students
                </Link>
              )}
              <Link href="/courses" className="btn btn-secondary">
                <BookOpenIcon className="h-5 w-5" />
                Courses
              </Link>
            </div>
            </div>
          </div>

        {/* Main Content */}
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {error && (
            <div className="mb-6 bg-red-50 border border-red-200 rounded-md p-4">
              <p className="text-red-600">{error}</p>
            </div>
          )}

          {/* Filters */}
          <div className="mb-6">
            <div className="flex items-center space-x-4">
              <label htmlFor="status-filter" className="text-sm font-medium text-gray-700">
                Filter by status:
              </label>
              <select
                id="status-filter"
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
                className="block border-gray-300 rounded-md shadow-sm focus:ring-primary-500 focus:border-primary-500 text-sm"
              >
                <option value="all">All Enrollments</option>
                <option value="active">Active</option>
                <option value="completed">Completed</option>
                <option value="inactive">Inactive</option>
              </select>
              <span className="text-sm text-gray-600">
                Showing {filteredEnrollments.length} of {enrollments.length} enrollments
              </span>
            </div>
          </div>

          {/* Enrollments */}
          {filteredEnrollments.length === 0 ? (
            <div className="text-center py-12">
              <ClipboardDocumentListIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">
                {enrollments.length === 0 ? 'No enrollments found' : 'No enrollments match the filter'}
              </h3>
              <p className="text-gray-600 mb-6">
                {enrollments.length === 0 
                  ? "No students are enrolled in any courses yet."
                  : "Try adjusting the filter to see more enrollments."
                }
              </p>
              <Link href="/courses" className="btn btn-primary">
                <BookOpenIcon className="h-5 w-5" />
                Browse Courses
              </Link>
            </div>
          ) : (
            <div className="space-y-4">
              {filteredEnrollments.map((enrollment) => (
                <div key={enrollment.id} className="bg-white rounded-lg shadow hover:shadow-md transition-shadow">
                  <div className="p-6">
                    <div className="flex items-start justify-between">
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center space-x-3 mb-2">
                          {getStatusIcon(enrollment.status, enrollment.isActive)}
                          <h3 className="text-lg font-semibold text-gray-900">
                            {enrollment.courseName}
                          </h3>
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(enrollment.status, enrollment.isActive)}`}>
                            {enrollment.isActive ? (enrollment.status || 'Active') : 'Inactive'}
                          </span>
                        </div>
                        
                        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 text-sm text-gray-600">
                          <div className="flex items-center">
                            <UserGroupIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                            <span>{enrollment.studentName}</span>
                          </div>
                          
                          <div className="flex items-center">
                            <CalendarIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                            <span>{enrollment.semesterName}</span>
                          </div>
                          
                          <div className="flex items-center">
                            <ClockIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                            <span>Enrolled: {formatDate(enrollment.enrollmentDate)}</span>
                          </div>
                          
                          {enrollment.completionDate && (
                            <div className="flex items-center">
                              <CheckCircleIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                              <span>Completed: {formatDate(enrollment.completionDate)}</span>
                            </div>
                          )}
                        </div>

                        {enrollment.finalGrade && (
                          <div className="mt-2">
                            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
                              Final Grade: {enrollment.finalGrade}
                            </span>
                          </div>
                        )}
                      </div>
                      
                      <div className="flex space-x-2 ml-4">
                        <button className="btn btn-secondary text-sm py-2 px-3">
                          View Details
                        </button>
                        {enrollment.isActive && (
                          <button className="btn btn-secondary text-sm py-2 px-3">
                            Manage
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>
    </ProtectedRoute>
  );
}
