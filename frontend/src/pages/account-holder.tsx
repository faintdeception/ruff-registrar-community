import React, { useState, useEffect } from 'react';
import { useRouter } from 'next/router';
import { useAuth } from '../lib/auth';
import ProtectedRoute from '../components/ProtectedRoute';
import apiClient from '../lib/api-client';
import { PlusIcon, XMarkIcon } from '@heroicons/react/24/outline';

interface AccountHolder {
  id: string;
  firstName: string;
  lastName: string;
  emailAddress: string;
  homePhone?: string;
  mobilePhone?: string;
  addressJson: {
    street: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
  };
  emergencyContactJson: {
    firstName: string;
    lastName: string;
    homePhone?: string;
    mobilePhone?: string;
    email: string;
  };
  membershipDuesOwed: number;
  membershipDuesReceived: number;
  memberSince: string;
  lastLogin?: string;
  lastEdit: string;
  students: Student[];
  payments: Payment[];
}

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
  enrollments: Enrollment[];
}

interface Enrollment {
  id: string;
  courseId: string;
  courseName: string;
  courseCode?: string;
  semesterName: string;
  enrollmentType: string;
  enrollmentDate: string;
  feeAmount: number;
  amountPaid: number;
  paymentStatus: string;
  waitlistPosition?: number;
  notes?: string;
}

interface Payment {
  id: string;
  amount: number;
  paymentDate: string;
  paymentMethod: string;
  paymentType: string;
  transactionId?: string;
  notes?: string;
}

interface CreateStudentForm {
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
}

const AccountHolderPage: React.FC = () => {
  const { user } = useAuth();
  const router = useRouter();
  const [accountHolder, setAccountHolder] = useState<AccountHolder | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [showAddStudentForm, setShowAddStudentForm] = useState(false);
  const [addingStudent, setAddingStudent] = useState(false);
  const [newStudent, setNewStudent] = useState<CreateStudentForm>({
    firstName: '',
    lastName: '',
    grade: '',
    dateOfBirth: '',
    studentInfoJson: {
      specialConditions: [],
      allergies: [],
      medications: [],
      preferredName: '',
      parentNotes: ''
    },
    notes: ''
  });

  useEffect(() => {
    if (!user) return;
    fetchAccountHolder();
  }, [user]);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  const handleAddStudent = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setError(null);
      setSuccessMessage(null);
      setAddingStudent(true);

      const response = await apiClient.post('/api/account-holders/me/students', {
        firstName: newStudent.firstName,
        lastName: newStudent.lastName,
        grade: newStudent.grade || null,
        dateOfBirth: newStudent.dateOfBirth ? new Date(newStudent.dateOfBirth).toISOString() : null,
        studentInfoJson: {
          specialConditions: newStudent.studentInfoJson.specialConditions?.filter(c => c.trim()) || [],
          allergies: newStudent.studentInfoJson.allergies?.filter(a => a.trim()) || [],
          medications: newStudent.studentInfoJson.medications?.filter(m => m.trim()) || [],
          preferredName: newStudent.studentInfoJson.preferredName || null,
          parentNotes: newStudent.studentInfoJson.parentNotes || null
        },
        notes: newStudent.notes || null
      });

      // Refresh account holder data
      fetchAccountHolder();
      
      // Show success message
      setSuccessMessage('Student added successfully!');
      setTimeout(() => setSuccessMessage(null), 5000);
      
      // Reset form
      setNewStudent({
        firstName: '',
        lastName: '',
        grade: '',
        dateOfBirth: '',
        studentInfoJson: {
          specialConditions: [],
          allergies: [],
          medications: [],
          preferredName: '',
          parentNotes: ''
        },
        notes: ''
      });
      setShowAddStudentForm(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to add student');
    } finally {
      setAddingStudent(false);
    }
  };

  const handleRemoveStudent = async (studentId: string) => {
    if (!window.confirm('Are you sure you want to remove this student?')) {
      return;
    }

    try {
      setError(null);
      setSuccessMessage(null);
      
      await apiClient.delete(`/api/account-holders/me/students/${studentId}`);

      // Refresh account holder data
      fetchAccountHolder();
      
      // Show success message
      setSuccessMessage('Student removed successfully!');
      setTimeout(() => setSuccessMessage(null), 5000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to remove student');
    }
  };

  const fetchAccountHolder = async () => {
    try {
      setError(null);
      setSuccessMessage(null);
      
      const response = await apiClient.get('/api/account-holders/me');
      
      if (!response.ok) {
        throw new Error('Failed to fetch account holder data');
      }
      
      const data = await response.json();
      setAccountHolder(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <div className="text-red-800">
            <h3 className="text-lg font-medium">Error</h3>
            <p className="mt-1">{error}</p>
          </div>
        </div>
      </div>
    );
  }

  if (!accountHolder) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-gray-500">No account holder data found</div>
      </div>
    );
  }

  const totalDuesBalance = accountHolder.membershipDuesOwed - accountHolder.membershipDuesReceived;

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        {/* Success Message */}
        {successMessage && (
          <div className="mb-6 p-4 bg-green-50 border border-green-200 rounded-lg">
            <p className="text-green-800">{successMessage}</p>
          </div>
        )}
        
        {/* Error Message */}
        {error && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
            <p className="text-red-800">{error}</p>
          </div>
        )}
        
        {/* Header */}
        <div className="bg-white shadow rounded-lg mb-6">
          <div className="px-4 py-5 sm:px-6">
            <div className="flex justify-between items-start">
              <div>
                <h1 className="text-2xl font-bold text-gray-900">
                  {accountHolder.firstName} {accountHolder.lastName}
                </h1>
                <p className="text-sm text-gray-500">
                  Member since {formatDate(accountHolder.memberSince)}
                </p>
              </div>
              <div className="text-right">
                <p className="text-sm text-gray-500">Last Login</p>
                <p className="text-sm font-medium">
                  {accountHolder.lastLogin ? formatDate(accountHolder.lastLogin) : 'Never'}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Account Information */}
          <div className="lg:col-span-2">
            <div className="bg-white shadow rounded-lg">
              <div className="px-4 py-5 sm:px-6">
                <h2 className="text-lg font-medium text-gray-900">Account Information</h2>
              </div>
              <div className="border-t border-gray-200">
                <dl className="divide-y divide-gray-200">
                  <div className="px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
                    <dt className="text-sm font-medium text-gray-500">Email</dt>
                    <dd className="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      {accountHolder.emailAddress}
                    </dd>
                  </div>
                  <div className="px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
                    <dt className="text-sm font-medium text-gray-500">Phone Numbers</dt>
                    <dd className="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      {accountHolder.homePhone && (
                        <div>Home: {accountHolder.homePhone}</div>
                      )}
                      {accountHolder.mobilePhone && (
                        <div>Mobile: {accountHolder.mobilePhone}</div>
                      )}
                    </dd>
                  </div>
                  <div className="px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
                    <dt className="text-sm font-medium text-gray-500">Address</dt>
                    <dd className="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      <div>{accountHolder.addressJson.street}</div>
                      <div>
                        {accountHolder.addressJson.city}, {accountHolder.addressJson.state} {accountHolder.addressJson.postalCode}
                      </div>
                      <div>{accountHolder.addressJson.country}</div>
                    </dd>
                  </div>
                  <div className="px-4 py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
                    <dt className="text-sm font-medium text-gray-500">Emergency Contact</dt>
                    <dd className="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      <div className="font-medium">
                        {accountHolder.emergencyContactJson.firstName} {accountHolder.emergencyContactJson.lastName}
                      </div>
                      <div>{accountHolder.emergencyContactJson.email}</div>
                      {accountHolder.emergencyContactJson.homePhone && (
                        <div>Home: {accountHolder.emergencyContactJson.homePhone}</div>
                      )}
                      {accountHolder.emergencyContactJson.mobilePhone && (
                        <div>Mobile: {accountHolder.emergencyContactJson.mobilePhone}</div>
                      )}
                    </dd>
                  </div>
                </dl>
              </div>
            </div>
          </div>

          {/* Membership Dues */}
          <div className="space-y-6">
            <div className="bg-white shadow rounded-lg">
              <div className="px-4 py-5 sm:px-6">
                <h2 className="text-lg font-medium text-gray-900">Membership Dues</h2>
              </div>
              <div className="border-t border-gray-200 px-4 py-5 sm:px-6">
                <dl className="space-y-3">
                  <div className="flex justify-between">
                    <dt className="text-sm text-gray-500">Total Owed</dt>
                    <dd className="text-sm font-medium text-gray-900">
                      {formatCurrency(accountHolder.membershipDuesOwed)}
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-sm text-gray-500">Total Received</dt>
                    <dd className="text-sm font-medium text-gray-900">
                      {formatCurrency(accountHolder.membershipDuesReceived)}
                    </dd>
                  </div>
                  <div className="flex justify-between border-t border-gray-200 pt-3">
                    <dt className="text-sm font-medium text-gray-500">Balance</dt>
                    <dd className={`text-sm font-medium ${
                      totalDuesBalance > 0 ? 'text-red-600' : 'text-green-600'
                    }`}>
                      {formatCurrency(totalDuesBalance)}
                    </dd>
                  </div>
                </dl>
              </div>
            </div>
          </div>
        </div>

        {/* Students */}
        <div className="mt-6">
          <div className="bg-white shadow rounded-lg">
            <div className="px-4 py-5 sm:px-6 flex justify-between items-center">
              <h2 className="text-lg font-medium text-gray-900">Students</h2>
              <button
                onClick={() => setShowAddStudentForm(true)}
                className="inline-flex items-center px-3 py-2 border border-transparent text-sm leading-4 font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                <PlusIcon className="h-4 w-4 mr-1" />
                Add Student
              </button>
            </div>
            
            {showAddStudentForm && (
              <div className="border-t border-gray-200 px-4 py-5 sm:px-6 bg-gray-50">
                <form onSubmit={handleAddStudent} className="space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        First Name *
                      </label>
                      <input
                        type="text"
                        required
                        value={newStudent.firstName}
                        onChange={(e) => setNewStudent({...newStudent, firstName: e.target.value})}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Last Name *
                      </label>
                      <input
                        type="text"
                        required
                        value={newStudent.lastName}
                        onChange={(e) => setNewStudent({...newStudent, lastName: e.target.value})}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Preferred Name
                      </label>
                      <input
                        type="text"
                        value={newStudent.studentInfoJson.preferredName}
                        onChange={(e) => setNewStudent({
                          ...newStudent, 
                          studentInfoJson: {...newStudent.studentInfoJson, preferredName: e.target.value}
                        })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Grade
                      </label>
                      <input
                        type="text"
                        value={newStudent.grade}
                        onChange={(e) => setNewStudent({...newStudent, grade: e.target.value})}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="e.g., K, 1, 2, 3..."
                      />
                    </div>
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Date of Birth
                      </label>
                      <input
                        type="date"
                        value={newStudent.dateOfBirth}
                        onChange={(e) => setNewStudent({...newStudent, dateOfBirth: e.target.value})}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Allergies (comma-separated)
                    </label>
                    <input
                      type="text"
                      value={newStudent.studentInfoJson.allergies?.join(', ') || ''}
                      onChange={(e) => setNewStudent({
                        ...newStudent, 
                        studentInfoJson: {
                          ...newStudent.studentInfoJson, 
                          allergies: e.target.value.split(',').map(a => a.trim()).filter(a => a)
                        }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="e.g., Peanuts, Tree nuts"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Medications (comma-separated)
                    </label>
                    <input
                      type="text"
                      value={newStudent.studentInfoJson.medications?.join(', ') || ''}
                      onChange={(e) => setNewStudent({
                        ...newStudent, 
                        studentInfoJson: {
                          ...newStudent.studentInfoJson, 
                          medications: e.target.value.split(',').map(m => m.trim()).filter(m => m)
                        }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="e.g., EpiPen, Inhaler"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Special Conditions (comma-separated)
                    </label>
                    <input
                      type="text"
                      value={newStudent.studentInfoJson.specialConditions?.join(', ') || ''}
                      onChange={(e) => setNewStudent({
                        ...newStudent, 
                        studentInfoJson: {
                          ...newStudent.studentInfoJson, 
                          specialConditions: e.target.value.split(',').map(c => c.trim()).filter(c => c)
                        }
                      })}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="e.g., ADHD, Autism"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Parent Notes
                    </label>
                    <textarea
                      value={newStudent.studentInfoJson.parentNotes}
                      onChange={(e) => setNewStudent({
                        ...newStudent, 
                        studentInfoJson: {...newStudent.studentInfoJson, parentNotes: e.target.value}
                      })}
                      rows={3}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="Any additional notes about your child..."
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      General Notes
                    </label>
                    <textarea
                      value={newStudent.notes}
                      onChange={(e) => setNewStudent({...newStudent, notes: e.target.value})}
                      rows={2}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="Any other notes..."
                    />
                  </div>
                  
                  <div className="flex justify-end space-x-3">
                    <button
                      type="button"
                      onClick={() => {
                        setShowAddStudentForm(false);
                        setNewStudent({
                          firstName: '',
                          lastName: '',
                          grade: '',
                          dateOfBirth: '',
                          studentInfoJson: {
                            specialConditions: [],
                            allergies: [],
                            medications: [],
                            preferredName: '',
                            parentNotes: ''
                          },
                          notes: ''
                        });
                      }}
                      className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={addingStudent}
                      className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
                    >
                      {addingStudent ? 'Adding...' : 'Add Student'}
                    </button>
                  </div>
                </form>
              </div>
            )}
            
            <div className="border-t border-gray-200">
              {accountHolder.students && accountHolder.students.length > 0 ? (
                <div className="space-y-6 px-4 py-5 sm:px-6">
                  {accountHolder.students.map((student) => (
                    <div key={student.id} className="border border-gray-200 rounded-lg p-4">
                      <div className="flex justify-between items-start mb-3">
                        <div>
                          <h3 className="text-lg font-medium text-gray-900">
                            {student.firstName} {student.lastName}
                            {student.studentInfoJson.preferredName && 
                              student.studentInfoJson.preferredName !== student.firstName && 
                              ` (${student.studentInfoJson.preferredName})`
                            }
                          </h3>
                          {student.grade && (
                            <p className="text-sm text-gray-500">Grade: {student.grade}</p>
                          )}
                          {student.dateOfBirth && (
                            <p className="text-sm text-gray-500">
                              Birth Date: {formatDate(student.dateOfBirth)}
                            </p>
                          )}
                        </div>
                        <button
                          onClick={() => handleRemoveStudent(student.id)}
                          className="inline-flex items-center px-2 py-1 border border-transparent text-xs font-medium rounded text-red-700 bg-red-100 hover:bg-red-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
                          title="Remove student"
                        >
                          <XMarkIcon className="h-4 w-4" />
                        </button>
                      </div>

                      {/* Student Info */}
                      {(student.studentInfoJson.allergies?.length || 
                        student.studentInfoJson.medications?.length || 
                        student.studentInfoJson.specialConditions?.length) && (
                        <div className="mb-3 p-3 bg-yellow-50 rounded">
                          <h4 className="text-sm font-medium text-yellow-800 mb-2">Important Information</h4>
                          {student.studentInfoJson.allergies?.length && (
                            <p className="text-sm text-yellow-700">
                              <strong>Allergies:</strong> {student.studentInfoJson.allergies.join(', ')}
                            </p>
                          )}
                          {student.studentInfoJson.medications?.length && (
                            <p className="text-sm text-yellow-700">
                              <strong>Medications:</strong> {student.studentInfoJson.medications.join(', ')}
                            </p>
                          )}
                          {student.studentInfoJson.specialConditions?.length && (
                            <p className="text-sm text-yellow-700">
                              <strong>Special Conditions:</strong> {student.studentInfoJson.specialConditions.join(', ')}
                            </p>
                          )}
                        </div>
                      )}

                      {/* Enrollments */}
                      {student.enrollments && student.enrollments.length > 0 && (
                        <div>
                          <h4 className="text-sm font-medium text-gray-900 mb-2">Current Enrollments</h4>
                          <div className="space-y-2">
                            {student.enrollments.map((enrollment) => (
                              <div key={enrollment.id} className="flex justify-between items-center p-2 bg-gray-50 rounded">
                                <div>
                                  <span className="text-sm font-medium">{enrollment.courseName}</span>
                                  {enrollment.courseCode && (
                                    <span className="text-sm text-gray-500 ml-2">({enrollment.courseCode})</span>
                                  )}
                                  <div className="text-xs text-gray-500">
                                    {enrollment.semesterName} • {enrollment.enrollmentType}
                                    {enrollment.waitlistPosition && (
                                      <span className="text-yellow-600"> • Waitlist #{enrollment.waitlistPosition}</span>
                                    )}
                                  </div>
                                </div>
                                <div className="text-right">
                                  <div className="text-sm font-medium">
                                    {formatCurrency(enrollment.feeAmount)}
                                  </div>
                                  <div className={`text-xs ${
                                    enrollment.paymentStatus === 'PAID' ? 'text-green-600' : 
                                    enrollment.paymentStatus === 'PARTIAL' ? 'text-yellow-600' : 
                                    'text-red-600'
                                  }`}>
                                    {enrollment.paymentStatus}
                                  </div>
                                </div>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <div className="px-4 py-5 sm:px-6 text-center text-gray-500">
                  No students added yet. Click "Add Student" to get started.
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Recent Payments */}
        {accountHolder.payments && accountHolder.payments.length > 0 && (
          <div className="mt-6">
            <div className="bg-white shadow rounded-lg">
              <div className="px-4 py-5 sm:px-6">
                <h2 className="text-lg font-medium text-gray-900">Recent Payments</h2>
              </div>
              <div className="border-t border-gray-200">
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Date
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Amount
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Method
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Type
                        </th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                          Transaction ID
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-200">
                      {accountHolder.payments.slice(0, 10).map((payment) => (
                        <tr key={payment.id}>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            {formatDate(payment.paymentDate)}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                            {formatCurrency(payment.amount)}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {payment.paymentMethod}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {payment.paymentType}
                          </td>
                          <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                            {payment.transactionId || '-'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default function AccountHolder() {
  return (
    <ProtectedRoute>
      <AccountHolderPage />
    </ProtectedRoute>
  );
}
