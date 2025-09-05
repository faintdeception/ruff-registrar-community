import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useAuth } from '@/lib/auth';
import ProtectedRoute from '@/components/ProtectedRoute';
import apiClient from '@/lib/api-client';
import {
  ChartBarIcon,
  UserGroupIcon,
  BookOpenIcon,
  CalendarIcon,
  AcademicCapIcon,
  TrophyIcon,
  ExclamationTriangleIcon
} from '@heroicons/react/24/outline';

interface Grade {
  id: string;
  gradeValue: string;
  gradeDate: string;
  gradingPeriod?: string;
  comments?: string;
  studentName: string;
  studentId: string;
  courseName: string;
  courseId: string;
  semesterName: string;
  semesterId: string;
  instructorName?: string;
}

interface GradeSummary {
  studentId: string;
  studentName: string;
  grades: Grade[];
  gpa?: number;
  totalCourses: number;
  completedCourses: number;
}

export default function GradesPage() {
  const { user } = useAuth();
  const [grades, setGrades] = useState<Grade[]>([]);
  const [gradeSummaries, setGradeSummaries] = useState<GradeSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'summary' | 'detailed'>('summary');

  const isAdmin = user?.roles.includes('Administrator');

  useEffect(() => {
    fetchGrades();
  }, []);

  const fetchGrades = async () => {
    try {
      setLoading(true);

      // Get students and their enrollments to extract grade information
      const response = await apiClient.get('/api/account-holders/me/students');

      if (!response.ok) {
        throw new Error('Failed to fetch grade data');
      }

      const students = await response.json();
      
      // Extract grades from enrollments
      const allGrades: Grade[] = [];
      const summaries: GradeSummary[] = [];

      students.forEach((student: any) => {
        const studentGrades: Grade[] = [];
        
        student.enrollments?.forEach((enrollment: any) => {
          // For now, we'll use the finalGrade from enrollment
          // In a real system, there would be separate grade records
          if (enrollment.finalGrade) {
            const grade: Grade = {
              id: `${enrollment.id}-final`,
              gradeValue: enrollment.finalGrade,
              gradeDate: enrollment.completionDate || enrollment.enrollmentDate,
              gradingPeriod: 'Final',
              studentName: `${student.firstName} ${student.lastName}`,
              studentId: student.id,
              courseName: enrollment.courseName || 'Unknown Course',
              courseId: enrollment.courseId || '',
              semesterName: enrollment.semesterName || 'Unknown Semester',
              semesterId: enrollment.semesterId || '',
              instructorName: enrollment.instructorName
            };
            allGrades.push(grade);
            studentGrades.push(grade);
          }
        });

        const summary: GradeSummary = {
          studentId: student.id,
          studentName: `${student.firstName} ${student.lastName}`,
          grades: studentGrades,
          totalCourses: student.enrollments?.length || 0,
          completedCourses: student.enrollments?.filter((e: any) => e.finalGrade)?.length || 0,
          gpa: calculateGPA(studentGrades)
        };
        summaries.push(summary);
      });

      setGrades(allGrades);
      setGradeSummaries(summaries);
    } catch (err) {
      setError('Failed to fetch grades');
      console.error('Error fetching grades:', err);
    } finally {
      setLoading(false);
    }
  };

  const calculateGPA = (grades: Grade[]): number | undefined => {
    if (grades.length === 0) return undefined;
    
    const gradePoints: { [key: string]: number } = {
      'A+': 4.0, 'A': 4.0, 'A-': 3.7,
      'B+': 3.3, 'B': 3.0, 'B-': 2.7,
      'C+': 2.3, 'C': 2.0, 'C-': 1.7,
      'D+': 1.3, 'D': 1.0, 'D-': 0.7,
      'F': 0.0
    };

    const validGrades = grades.filter(g => gradePoints[g.gradeValue] !== undefined);
    if (validGrades.length === 0) return undefined;

    const totalPoints = validGrades.reduce((sum, grade) => sum + gradePoints[grade.gradeValue], 0);
    return totalPoints / validGrades.length;
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getGradeColor = (grade: string) => {
    switch (grade.charAt(0)) {
      case 'A': return 'bg-green-100 text-green-800';
      case 'B': return 'bg-blue-100 text-blue-800';
      case 'C': return 'bg-yellow-100 text-yellow-800';
      case 'D': return 'bg-orange-100 text-orange-800';
      case 'F': return 'bg-red-100 text-red-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getGPAColor = (gpa: number) => {
    if (gpa >= 3.5) return 'text-green-600';
    if (gpa >= 3.0) return 'text-blue-600';
    if (gpa >= 2.5) return 'text-yellow-600';
    if (gpa >= 2.0) return 'text-orange-600';
    return 'text-red-600';
  };

  if (loading) {
    return (
      <ProtectedRoute>
        <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center">
            <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-primary-600 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading grades...</p>
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
              <ChartBarIcon className="h-8 w-8 text-primary-600" />
              <h1 className="ml-3 text-2xl font-bold text-gray-900">Grades</h1>
            </div>
            <div className="flex space-x-3">
              <div className="flex rounded-md shadow-sm">
                <button
                  onClick={() => setViewMode('summary')}
                  className={`px-4 py-2 text-sm font-medium border ${
                    viewMode === 'summary'
                      ? 'bg-primary-600 text-white border-primary-600'
                      : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
                  } rounded-l-md`}
                >
                  Summary
                </button>
                <button
                  onClick={() => setViewMode('detailed')}
                  className={`px-4 py-2 text-sm font-medium border ${
                    viewMode === 'detailed'
                      ? 'bg-primary-600 text-white border-primary-600'
                      : 'bg-white text-gray-700 border-gray-300 hover:bg-gray-50'
                  } rounded-r-md border-l-0`}
                >
                  Detailed
                </button>
              </div>
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
          {error && (
            <div className="mb-6 bg-red-50 border border-red-200 rounded-md p-4">
              <p className="text-red-600">{error}</p>
            </div>
          )}

          {/* Summary View */}
          {viewMode === 'summary' && (
            <div>
              {gradeSummaries.length === 0 ? (
                <div className="text-center py-12">
                  <ChartBarIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No grades found</h3>
                  <p className="text-gray-600 mb-6">
                    No students have completed courses with grades yet.
                  </p>
                  <Link href="/enrollments" className="btn btn-primary">
                    <BookOpenIcon className="h-5 w-5" />
                    View Enrollments
                  </Link>
                </div>
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {gradeSummaries.map((summary) => (
                    <div key={summary.studentId} className="bg-white rounded-lg shadow hover:shadow-md transition-shadow">
                      <div className="p-6">
                        <div className="flex items-start justify-between mb-4">
                          <div>
                            <h3 className="text-lg font-semibold text-gray-900">
                              {summary.studentName}
                            </h3>
                            <p className="text-sm text-gray-600">
                              {summary.completedCourses} of {summary.totalCourses} courses completed
                            </p>
                          </div>
                          {summary.gpa !== undefined && (
                            <div className="text-right">
                              <div className="flex items-center">
                                <TrophyIcon className="h-5 w-5 text-yellow-500 mr-1" />
                                <span className={`text-lg font-bold ${getGPAColor(summary.gpa)}`}>
                                  {summary.gpa.toFixed(2)}
                                </span>
                              </div>
                              <p className="text-xs text-gray-500">GPA</p>
                            </div>
                          )}
                        </div>

                        {summary.grades.length > 0 ? (
                          <div>
                            <h4 className="text-sm font-medium text-gray-700 mb-2">Recent Grades:</h4>
                            <div className="space-y-1">
                              {summary.grades.slice(-3).map((grade) => (
                                <div key={grade.id} className="flex items-center justify-between text-sm">
                                  <span className="truncate flex-1 mr-2">{grade.courseName}</span>
                                  <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getGradeColor(grade.gradeValue)}`}>
                                    {grade.gradeValue}
                                  </span>
                                </div>
                              ))}
                            </div>
                            {summary.grades.length > 3 && (
                              <p className="text-xs text-gray-500 mt-2">
                                +{summary.grades.length - 3} more grades
                              </p>
                            )}
                          </div>
                        ) : (
                          <div className="flex items-center text-sm text-gray-500">
                            <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                            <span>No grades recorded</span>
                          </div>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Detailed View */}
          {viewMode === 'detailed' && (
            <div>
              {grades.length === 0 ? (
                <div className="text-center py-12">
                  <ChartBarIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No grades found</h3>
                  <p className="text-gray-600 mb-6">
                    No individual grades have been recorded yet.
                  </p>
                  <Link href="/enrollments" className="btn btn-primary">
                    <BookOpenIcon className="h-5 w-5" />
                    View Enrollments
                  </Link>
                </div>
              ) : (
                <div className="space-y-4">
                  {grades
                    .sort((a, b) => new Date(b.gradeDate).getTime() - new Date(a.gradeDate).getTime())
                    .map((grade) => (
                    <div key={grade.id} className="bg-white rounded-lg shadow hover:shadow-md transition-shadow">
                      <div className="p-6">
                        <div className="flex items-start justify-between">
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center space-x-3 mb-2">
                              <h3 className="text-lg font-semibold text-gray-900">
                                {grade.courseName}
                              </h3>
                              <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${getGradeColor(grade.gradeValue)}`}>
                                {grade.gradeValue}
                              </span>
                              {grade.gradingPeriod && (
                                <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800">
                                  {grade.gradingPeriod}
                                </span>
                              )}
                            </div>
                            
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 text-sm text-gray-600 mb-3">
                              <div className="flex items-center">
                                <UserGroupIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                                <span>{grade.studentName}</span>
                              </div>
                              
                              <div className="flex items-center">
                                <CalendarIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                                <span>{grade.semesterName}</span>
                              </div>
                              
                              <div className="flex items-center">
                                <ChartBarIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                                <span>Recorded: {formatDate(grade.gradeDate)}</span>
                              </div>
                              
                              {grade.instructorName && (
                                <div className="flex items-center">
                                  <AcademicCapIcon className="h-4 w-4 mr-2 flex-shrink-0" />
                                  <span>{grade.instructorName}</span>
                                </div>
                              )}
                            </div>

                            {grade.comments && (
                              <div className="mt-3">
                                <h4 className="text-sm font-medium text-gray-700 mb-1">Comments:</h4>
                                <p className="text-sm text-gray-600">{grade.comments}</p>
                              </div>
                            )}
                          </div>
                          
                          <div className="flex space-x-2 ml-4">
                            <button className="btn btn-secondary text-sm py-2 px-3">
                              View Details
                            </button>
                            {isAdmin && (
                              <button className="btn btn-secondary text-sm py-2 px-3">
                                Edit
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
          )}
      </main>
    </ProtectedRoute>
  );
}
