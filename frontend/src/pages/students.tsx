import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useAuth } from '@/lib/auth';
import ProtectedRoute from '@/components/ProtectedRoute';
import apiClient from '@/lib/api-client';
import {
  UserGroupIcon,
  PlusIcon,
  AcademicCapIcon,
  XMarkIcon,
  XCircleIcon,
  CalendarIcon,
  BookOpenIcon
} from '@heroicons/react/24/outline';

interface Student {
  id: string;
  firstName: string;
  lastName: string;
  grade?: string;
  dateOfBirth?: string;
  studentInfoJson: {
    specialConditions?: string[];
    allergies?: string[];
    medications?: string[];
    preferredName?: string;
    parentNotes?: string;
  };
  notes?: string;
  enrollments: any[];
  accountHolderName: string;
}

export default function StudentsPage() {
  const { user } = useAuth();
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isAdmin = user?.roles.includes('Administrator');

  useEffect(() => {
    fetchStudents();
  }, []);

  const fetchStudents = async () => {
    try {
      setLoading(true);

      // For regular users, get their students through account holder endpoint
      // For admins, we'd need a different endpoint to get all students
      const endpoint = isAdmin ? '/api/students' : '/api/account-holders/me/students';
      
      const response = await apiClient.get(endpoint);
      
      if (!response.ok) {
        throw new Error('Failed to fetch students');
      }
      
      const data = await response.json();
      setStudents(Array.isArray(data) ? data : []);
    } catch (err) {
      setError('Failed to fetch students');
      console.error('Error fetching students:', err);
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'Not provided';
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const calculateAge = (dateOfBirth?: string) => {
    if (!dateOfBirth) return null;
    const today = new Date();
    const birthDate = new Date(dateOfBirth);
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  };

  if (!isAdmin) {
    return (
      <ProtectedRoute>
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center">
            <XCircleIcon className="h-12 w-12 text-red-600 mx-auto mb-4" />
            <h2 className="text-xl font-semibold text-gray-900 mb-2">Access Denied</h2>
            <p className="text-gray-600">You need administrator privileges to access this page.</p>
          </div>
        </main>
      </ProtectedRoute>
    );
  }

  if (loading) {
    return (
      <ProtectedRoute>
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading students...</p>
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
              <UserGroupIcon className="h-8 w-8 text-primary-600" />
              <h1 className="ml-3 text-2xl font-bold text-gray-900">Students</h1>
            </div>
            <div className="flex space-x-3">
              <Link href="/account-holder" className="btn btn-secondary">
                <AcademicCapIcon className="h-5 w-5" />
                Account Management
              </Link>
              <Link href="/courses" className="btn btn-secondary">
                <BookOpenIcon className="h-5 w-5" />
                Browse Courses
              </Link>
            </div>
          </div>
        </div>
          {error && (
            <div className="mb-6 bg-red-50 border border-red-200 rounded-md p-4">
              <p className="text-red-600">{error}</p>
            </div>
          )}

          {/* Students Grid */}
          {students.length === 0 ? (
            <div className="text-center py-12">
              <UserGroupIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No students found</h3>
              <p className="text-gray-600 mb-6">
                {isAdmin 
                  ? "No students are registered in the system." 
                  : "You don't have any students registered yet."
                }
              </p>
              <Link href="/account-holder" className="btn btn-primary">
                <PlusIcon className="h-5 w-5" />
                Add Student
              </Link>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {students.map((student) => (
                <div key={student.id} className="bg-white rounded-lg shadow hover:shadow-md transition-shadow">
                  <div className="p-6">
                    {/* Student Header */}
                    <div className="mb-4">
                      <div className="flex items-start justify-between">
                        <div className="flex-1 min-w-0">
                          <h3 className="text-lg font-semibold text-gray-900">
                            {student.studentInfoJson?.preferredName || student.firstName} {student.lastName}
                          </h3>
                          {student.firstName !== student.studentInfoJson?.preferredName && student.studentInfoJson?.preferredName && (
                            <p className="text-sm text-gray-600">
                              Legal name: {student.firstName} {student.lastName}
                            </p>
                          )}
                        </div>
                        {student.grade && (
                          <span className="ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                            {student.grade}
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Student Details */}
                    <div className="space-y-2 mb-4">
                      {student.dateOfBirth && (
                        <div className="flex items-center text-sm text-gray-600">
                          <CalendarIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                          <span>
                            {formatDate(student.dateOfBirth)}
                            {calculateAge(student.dateOfBirth) && (
                              <span className="ml-1">({calculateAge(student.dateOfBirth)} years old)</span>
                            )}
                          </span>
                        </div>
                      )}

                      {isAdmin && student.accountHolderName && (
                        <div className="flex items-center text-sm text-gray-600">
                          <AcademicCapIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                          <span>Account: {student.accountHolderName}</span>
                        </div>
                      )}
                    </div>

                    {/* Special Conditions */}
                    {(student.studentInfoJson?.allergies?.length || 
                      student.studentInfoJson?.medications?.length || 
                      student.studentInfoJson?.specialConditions?.length) && (
                      <div className="mb-4">
                        <h4 className="text-sm font-medium text-gray-700 mb-2">Important Information:</h4>
                        <div className="space-y-1">
                          {student.studentInfoJson?.allergies?.map((allergy, index) => (
                            <span key={index} className="inline-block bg-red-100 text-red-800 text-xs px-2 py-1 rounded mr-1 mb-1">
                              Allergy: {allergy}
                            </span>
                          ))}
                          {student.studentInfoJson?.medications?.map((medication, index) => (
                            <span key={index} className="inline-block bg-yellow-100 text-yellow-800 text-xs px-2 py-1 rounded mr-1 mb-1">
                              Medication: {medication}
                            </span>
                          ))}
                          {student.studentInfoJson?.specialConditions?.map((condition, index) => (
                            <span key={index} className="inline-block bg-blue-100 text-blue-800 text-xs px-2 py-1 rounded mr-1 mb-1">
                              {condition}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}

                    {/* Notes */}
                    {(student.notes || student.studentInfoJson?.parentNotes) && (
                      <div className="mb-4">
                        <h4 className="text-sm font-medium text-gray-700 mb-1">Notes:</h4>
                        <p className="text-sm text-gray-600">
                          {student.notes || student.studentInfoJson?.parentNotes}
                        </p>
                      </div>
                    )}

                    {/* Actions */}
                    <div className="flex space-x-2">
                      <button className="btn btn-secondary text-sm py-2 px-3">
                        Edit
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
      </main>
    </ProtectedRoute>
  );
}
