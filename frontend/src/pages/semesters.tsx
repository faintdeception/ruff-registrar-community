import { useState, useEffect } from 'react';
import { useAuth } from '@/lib/auth';
import ProtectedRoute from '@/components/ProtectedRoute';
import {
  CalendarIcon,
  PlusIcon,
  PencilIcon,
  TrashIcon,
  CheckCircleIcon,
  XCircleIcon,
  BookOpenIcon,
  ClockIcon
} from '@heroicons/react/24/outline';

interface Semester {
  id: string;
  name: string;
  code: string;
  startDate: string;
  endDate: string;
  registrationStartDate: string;
  registrationEndDate: string;
  isActive: boolean;
  courseCount: number;
  createdAt: string;
  updatedAt: string;
}

interface CreateSemesterDto {
  name: string;
  code: string;
  startDate: string;
  endDate: string;
  registrationStartDate: string;
  registrationEndDate: string;
  isActive: boolean;
}

export default function SemestersPage() {
  const { user } = useAuth();
  const [semesters, setSemesters] = useState<Semester[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingSemester, setEditingSemester] = useState<Semester | null>(null);
  const [formData, setFormData] = useState<CreateSemesterDto>({
    name: '',
    code: '',
    startDate: '',
    endDate: '',
    registrationStartDate: '',
    registrationEndDate: '',
    isActive: false
  });
  const [submitting, setSubmitting] = useState(false);
  const [debugClaims, setDebugClaims] = useState<any>(null);

  const isAdmin = user?.roles.includes('Administrator');

  useEffect(() => {
    if (isAdmin) {
      fetchSemesters();
      // Debug: fetch claims to see what's happening
      debugFetchClaims();
    }
  }, [isAdmin]);

  const debugFetchClaims = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      if (!token) return;

      const response = await fetch('/api/debug/claims', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const claims = await response.json();
        setDebugClaims(claims);
        console.log('Debug Claims:', claims);
      }
    } catch (err) {
      console.error('Error fetching debug claims:', err);
    }
  };

  const fetchSemesters = async () => {
    try {
      setLoading(true);
      
      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      const response = await fetch('/api/semesters', {
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to fetch semesters');
      }

      const data = await response.json();
      setSemesters(data);
    } catch (err) {
      setError('Failed to fetch semesters');
      console.error('Error fetching semesters:', err);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({
      name: '',
      code: '',
      startDate: '',
      endDate: '',
      registrationStartDate: '',
      registrationEndDate: '',
      isActive: false
    });
    setEditingSemester(null);
  };

  const openCreateModal = () => {
    resetForm();
    setShowCreateModal(true);
  };

  const openEditModal = (semester: Semester) => {
    setFormData({
      name: semester.name,
      code: semester.code || '',
      startDate: semester.startDate.split('T')[0],
      endDate: semester.endDate.split('T')[0],
      registrationStartDate: semester.registrationStartDate.split('T')[0],
      registrationEndDate: semester.registrationEndDate.split('T')[0],
      isActive: semester.isActive
    });
    setEditingSemester(semester);
    setShowCreateModal(true);
  };

  const closeModal = () => {
    setShowCreateModal(false);
    resetForm();
    setError(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      // Validate dates
      const startDate = new Date(formData.startDate);
      const endDate = new Date(formData.endDate);
      const regStartDate = new Date(formData.registrationStartDate);
      const regEndDate = new Date(formData.registrationEndDate);

      if (endDate <= startDate) {
        throw new Error('End date must be after start date');
      }
      if (regEndDate <= regStartDate) {
        throw new Error('Registration end date must be after registration start date');
      }
      if (regStartDate > startDate) {
        throw new Error('Registration should start before or on the semester start date');
      }

      const submitData = {
        ...formData,
        startDate: startDate.toISOString(),
        endDate: endDate.toISOString(),
        registrationStartDate: regStartDate.toISOString(),
        registrationEndDate: regEndDate.toISOString()
      };

      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      let response;
      if (editingSemester) {
        response = await fetch(`/api/semesters/${editingSemester.id}`, {
          method: 'PUT',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(submitData),
        });
      } else {
        response = await fetch('/api/semesters', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(submitData),
        });
      }

      if (!response.ok) {
        throw new Error('Failed to save semester');
      }

      await fetchSemesters();
      closeModal();
    } catch (err: any) {
      setError(err.message || 'Failed to save semester');
      console.error('Error saving semester:', err);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (semesterId: string, semesterName: string) => {
    if (!confirm(`Are you sure you want to delete the semester "${semesterName}"? This action cannot be undone.`)) {
      return;
    }

    try {
      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      const response = await fetch(`/api/semesters/${semesterId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to delete semester');
      }

      await fetchSemesters();
    } catch (err) {
      setError('Failed to delete semester');
      console.error('Error deleting semester:', err);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getSemesterStatus = (semester: Semester) => {
    const now = new Date();
    const startDate = new Date(semester.startDate);
    const endDate = new Date(semester.endDate);
    const regStartDate = new Date(semester.registrationStartDate);
    const regEndDate = new Date(semester.registrationEndDate);

    if (now < regStartDate) {
      return { status: 'upcoming', label: 'Upcoming', color: 'bg-blue-100 text-blue-800' };
    } else if (now >= regStartDate && now <= regEndDate) {
      return { status: 'registration', label: 'Registration Open', color: 'bg-green-100 text-green-800' };
    } else if (now > regEndDate && now < startDate) {
      return { status: 'pre-semester', label: 'Registration Closed', color: 'bg-yellow-100 text-yellow-800' };
    } else if (now >= startDate && now <= endDate) {
      return { status: 'active', label: 'Active', color: 'bg-green-100 text-green-800' };
    } else {
      return { status: 'completed', label: 'Completed', color: 'bg-gray-100 text-gray-800' };
    }
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
            <p className="mt-4 text-gray-600">Loading semesters...</p>
          </div>
        </main>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute>
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="bg-white shadow">
          <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
            <div className="flex items-center justify-between">
              <div className="flex items-center">
                <CalendarIcon className="h-8 w-8 text-primary-600" />
                <h1 className="ml-3 text-2xl font-bold text-gray-900">Semester Management</h1>
              </div>
              <button 
                id="create-semester-btn"
                onClick={openCreateModal} 
                className="btn btn-primary"
              >
                <PlusIcon className="h-5 w-5" />
                Create Semester
              </button>
            </div>
          </div>
        </div>

        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          {error && (
            <div className="mb-6 bg-red-50 border border-red-200 rounded-md p-4">
              <p className="text-red-600">{error}</p>
            </div>
          )}

          {/* Semesters List */}
          {semesters.length === 0 ? (
            <div className="text-center py-12">
              <CalendarIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No semesters found</h3>
              <p className="text-gray-600 mb-6">
                Create your first semester to start organizing courses.
              </p>
              <button 
                id="create-first-semester-btn"
                onClick={openCreateModal} 
                className="btn btn-primary"
              >
                <PlusIcon className="h-5 w-5" />
                Create First Semester
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {semesters.map((semester) => {
                const status = getSemesterStatus(semester);
                const slug = semester.name.replace(/\s+/g, '-').toLowerCase();
                return (
                  <div 
                    key={semester.id} 
                    // Slug-based id for stable E2E testing
                    id={`semester-${slug}`}
                    // Preserve original data-testid (backwards compatibility) and expose the underlying DB id
                    data-testid={`semester-${slug}`}
                    data-semester-id={semester.id}
                    className="bg-white rounded-lg shadow hover:shadow-md transition-shadow"
                  >
                    <div className="p-6">
                      {/* Header */}
                      <div className="flex items-start justify-between mb-4">
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center">
                            <h3 className="text-lg font-semibold text-gray-900 truncate">
                              {semester.name}
                            </h3>
                            {semester.isActive && (
                              <CheckCircleIcon className="h-5 w-5 text-green-500 ml-2 flex-shrink-0" />
                            )}
                          </div>
                          {semester.code && (
                            <p className="text-sm text-gray-600 font-mono">{semester.code}</p>
                          )}
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${status.color} mt-2`}>
                            {status.label}
                          </span>
                        </div>
                        <div className="flex space-x-2 ml-4">
                          <button
                            id={`edit-semester-${semester.id}`}
                            onClick={() => openEditModal(semester)}
                            className="text-gray-400 hover:text-gray-600"
                          >
                            <PencilIcon className="h-5 w-5" />
                          </button>
                          <button
                            id={`delete-semester-${semester.id}`}
                            onClick={() => handleDelete(semester.id, semester.name)}
                            className="text-gray-400 hover:text-red-600"
                          >
                            <TrashIcon className="h-5 w-5" />
                          </button>
                        </div>
                      </div>

                      {/* Dates */}
                      <div className="space-y-3 mb-4">
                        <div>
                          <div className="flex items-center text-sm text-gray-600 mb-1">
                            <CalendarIcon className="h-4 w-4 mr-2" />
                            <span className="font-medium">Semester Period</span>
                          </div>
                          <p className="text-sm text-gray-900 ml-6">
                            {formatDate(semester.startDate)} - {formatDate(semester.endDate)}
                          </p>
                        </div>
                        
                        <div>
                          <div className="flex items-center text-sm text-gray-600 mb-1">
                            <ClockIcon className="h-4 w-4 mr-2" />
                            <span className="font-medium">Registration Period</span>
                          </div>
                          <p className="text-sm text-gray-900 ml-6">
                            {formatDate(semester.registrationStartDate)} - {formatDate(semester.registrationEndDate)}
                          </p>
                        </div>
                      </div>

                      {/* Stats */}
                      <div className="flex items-center justify-between pt-4 border-t border-gray-200">
                        <div className="flex items-center text-sm text-gray-600">
                          <BookOpenIcon className="h-4 w-4 mr-2" />
                          <span>{semester.courseCount} course{semester.courseCount !== 1 ? 's' : ''}</span>
                        </div>
                        <div className="text-xs text-gray-500">
                          Created {formatDate(semester.createdAt)}
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Create/Edit Modal */}
        {showCreateModal && (
          <div id="semester-modal" className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
            <div className="relative top-20 mx-auto p-5 border w-full max-w-2xl shadow-lg rounded-md bg-white">
              <div className="mt-3">
                <h3 id="modal-title" className="text-lg font-medium text-gray-900 mb-4">
                  {editingSemester ? 'Edit Semester' : 'Create New Semester'}
                </h3>
                
                <form onSubmit={handleSubmit} className="space-y-4">
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Semester Name *
                      </label>
                      <input
                        id="semester-name-input"
                        type="text"
                        required
                        value={formData.name}
                        onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                        className="form-input"
                        placeholder="e.g., Fall 2025"
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Semester Code
                      </label>
                      <input
                        id="semester-code-input"
                        type="text"
                        value={formData.code}
                        onChange={(e) => setFormData({ ...formData, code: e.target.value })}
                        className="form-input"
                        placeholder="e.g., FALL2025"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Start Date *
                      </label>
                      <input
                        id="semester-start-date-input"
                        type="date"
                        required
                        value={formData.startDate}
                        onChange={(e) => setFormData({ ...formData, startDate: e.target.value })}
                        className="form-input"
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        End Date *
                      </label>
                      <input
                        id="semester-end-date-input"
                        type="date"
                        required
                        value={formData.endDate}
                        onChange={(e) => setFormData({ ...formData, endDate: e.target.value })}
                        className="form-input"
                      />
                    </div>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Registration Start *
                      </label>
                      <input
                        id="semester-reg-start-date-input"
                        type="date"
                        required
                        value={formData.registrationStartDate}
                        onChange={(e) => setFormData({ ...formData, registrationStartDate: e.target.value })}
                        className="form-input"
                      />
                    </div>
                    
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Registration End *
                      </label>
                      <input
                        id="semester-reg-end-date-input"
                        type="date"
                        required
                        value={formData.registrationEndDate}
                        onChange={(e) => setFormData({ ...formData, registrationEndDate: e.target.value })}
                        className="form-input"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="flex items-center">
                      <input
                        id="semester-is-active-checkbox"
                        type="checkbox"
                        checked={formData.isActive}
                        onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                        className="form-checkbox"
                      />
                      <span className="ml-2 text-sm text-gray-700">
                        Set as active semester
                      </span>
                    </label>
                    <p className="text-xs text-gray-500 mt-1">
                      The active semester will be shown by default on the courses page
                    </p>
                  </div>

                  {error && (
                    <div id="error-message" className="bg-red-50 border border-red-200 rounded-md p-3">
                      <p className="text-red-600 text-sm">{error}</p>
                    </div>
                  )}

                  <div className="flex justify-end space-x-3 pt-4">
                    <button
                      id="cancel-semester-btn"
                      type="button"
                      onClick={closeModal}
                      disabled={submitting}
                      className="btn btn-secondary"
                    >
                      Cancel
                    </button>
                    <button
                      id="save-semester-btn"
                      type="submit"
                      disabled={submitting}
                      className="btn btn-primary"
                    >
                      {submitting ? 'Saving...' : editingSemester ? 'Update Semester' : 'Create Semester'}
                    </button>
                  </div>
                </form>
              </div>
            </div>
          </div>
        )}
      </main>
    </ProtectedRoute>
  );
}
