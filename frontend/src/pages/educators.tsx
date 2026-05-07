import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '@/lib/auth';
import ProtectedRoute from '@/components/ProtectedRoute';
import apiClient from '@/lib/api-client';
import { AccountHolderDto, EducatorDto, InviteEducatorDto, InviteEducatorResponse, UpdateEducatorDto, UserCredentials } from '@/types';

const emptyEducatorInvite: InviteEducatorDto = {
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  educatorInfo: {
    bio: '',
    qualifications: [],
    specializations: [],
    department: '',
    customFields: {}
  }
};

const EducatorsPage = () => {
  const { user } = useAuth();
  const [educators, setEducators] = useState<EducatorDto[]>([]);
  const [accountHolders, setAccountHolders] = useState<AccountHolderDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [inviteMessage, setInviteMessage] = useState<string | null>(null);
  const [inviteCredentials, setInviteCredentials] = useState<UserCredentials | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [newEducator, setNewEducator] = useState<InviteEducatorDto>(emptyEducatorInvite);
  const [editingEducator, setEditingEducator] = useState<EducatorDto | null>(null);
  const [editForm, setEditForm] = useState<UpdateEducatorDto | null>(null);
  const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

  const isAdmin = user?.roles?.includes('Administrator');

  const fetchEducators = useCallback(async () => {
    try {
      const response = await apiClient.get('/api/Educators');
      
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
  }, []);

  const fetchAccountHolders = useCallback(async () => {
    try {
      const response = await apiClient.get('/api/AccountHolders');

      if (response.ok) {
        const data = await response.json();
        setAccountHolders(data);
      }
    } catch (err) {
      console.error('Error loading account holders:', err);
    }
  }, []);

  useEffect(() => {
    fetchEducators();
  }, [fetchEducators]);

  useEffect(() => {
    if (isAdmin) {
      fetchAccountHolders();
    }
  }, [fetchAccountHolders, isAdmin]);

  const resetInviteForm = () => {
    setNewEducator(emptyEducatorInvite);
  };

  const handleAccountHolderSelect = (accountHolderId: string) => {
    const accountHolder = accountHolders.find(holder => holder.id === accountHolderId);

    if (!accountHolder) {
      setNewEducator({
        ...emptyEducatorInvite,
        educatorInfo: newEducator.educatorInfo
      });
      return;
    }

    setNewEducator({
      ...newEducator,
      accountHolderId,
      firstName: accountHolder.firstName,
      lastName: accountHolder.lastName,
      email: accountHolder.emailAddress,
      phone: accountHolder.mobilePhone || accountHolder.homePhone || ''
    });
  };

  const handleAddEducator = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      setInviteCredentials(null);
      setInviteMessage(null);
      setError(null);
      const response = await apiClient.post('/api/Educators/invite', newEducator);

      if (response.ok) {
        const invitation = await response.json() as InviteEducatorResponse;
        setEducators((currentEducators) => {
          const existingIndex = currentEducators.findIndex((educator) => educator.id === invitation.educator.id);
          if (existingIndex < 0) {
            return [...currentEducators, invitation.educator];
          }

          return currentEducators.map((educator, index) => index === existingIndex ? invitation.educator : educator);
        });
        setInviteCredentials(invitation.credentials || null);
        setInviteMessage(invitation.message || 'Educator authorized successfully.');
        setShowAddForm(false);
        resetInviteForm();
      } else {
        setError('Failed to invite educator');
      }
    } catch (err) {
      setError('Error inviting educator');
      console.error('Error inviting educator:', err);
    }
  };

  const handleToggleActive = async (id: string, currentStatus: boolean) => {
    if (!isAdmin) return;
    
    try {
      const endpoint = currentStatus ? 'deactivate' : 'activate';
      const response = await apiClient.post(`/api/Educators/${id}/${endpoint}`);

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

  const handleEditClick = (educator: EducatorDto) => {
    setEditingEducator(educator);
    setEditForm({
      firstName: educator.firstName,
      lastName: educator.lastName,
      email: educator.email || '',
      phone: educator.phone || '',
      educatorInfo: educator.educatorInfo
        ? { ...educator.educatorInfo }
        : { bio: '', qualifications: [], specializations: [], department: '', customFields: {} }
    });
  };

  const handleEditSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingEducator || !editForm) return;

    try {
      setError(null);
      const response = await apiClient.put(`/api/Educators/${editingEducator.id}`, editForm);

      if (response.ok) {
        const updated: EducatorDto = await response.json();
        setEducators(current => current.map(ed => ed.id === updated.id ? updated : ed));
        setEditingEducator(null);
        setEditForm(null);
      } else {
        setError('Failed to update educator');
      }
    } catch (err) {
      setError('Error updating educator');
      console.error('Error updating educator:', err);
    }
  };

  const handleDeleteClick = (id: string) => {
    setDeleteConfirmId(id);
  };

  const handleDeleteConfirm = async () => {
    if (!deleteConfirmId) return;

    try {
      setError(null);
      const response = await apiClient.delete(`/api/Educators/${deleteConfirmId}`);

      if (response.status === 204) {
        // Hard delete — remove from list
        setEducators(current => current.filter(ed => ed.id !== deleteConfirmId));
      } else if (response.status === 200) {
        // Soft delete — mark as deleted in list
        const body = await response.json() as { softDeleted: boolean };
        if (body.softDeleted) {
          setEducators(current =>
            current.map(ed =>
              ed.id === deleteConfirmId ? { ...ed, isDeleted: true, isActive: false } : ed
            )
          );
        }
      } else if (response.status === 404) {
        setError('Educator not found');
      } else {
        setError('Failed to delete educator');
      }
    } catch (err) {
      setError('Error deleting educator');
      console.error('Error deleting educator:', err);
    } finally {
      setDeleteConfirmId(null);
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
              id="add-educator-btn"
              data-testid="add-educator-btn"
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

        {inviteMessage && (
          <div className="bg-green-50 border border-green-200 text-green-900 px-4 py-3 rounded mb-4" data-testid="educator-invite-credentials">
            <div className="font-semibold" data-testid="educator-invite-message">{inviteMessage}</div>
            {inviteCredentials && (
              <>
                <div>Username: <span data-testid="educator-invite-username">{inviteCredentials.username}</span></div>
                <div>Temporary password: <span data-testid="educator-invite-password">{inviteCredentials.temporaryPassword}</span></div>
                {inviteCredentials.mustChangePassword && (
                  <div className="text-sm text-green-700">The educator must change this password on first login.</div>
                )}
              </>
            )}
          </div>
        )}

        {/* Add Educator Form */}
        {showAddForm && (
          <div className="mb-8 p-6 bg-gray-50 rounded-lg">
            <h2 className="text-xl font-semibold mb-4">Invite New Educator</h2>
            <form onSubmit={handleAddEducator} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Existing Parent or Member
                  </label>
                  <select
                    id="educator-account-holder-select"
                    data-testid="educator-account-holder-select"
                    value={newEducator.accountHolderId || ''}
                    onChange={(e) => handleAccountHolderSelect(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="">Invite a new educator...</option>
                    {accountHolders.map((accountHolder) => (
                      <option key={accountHolder.id} value={accountHolder.id}>
                        {accountHolder.firstName} {accountHolder.lastName} ({accountHolder.emailAddress})
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    First Name *
                  </label>
                  <input
                    id="educator-first-name-input"
                    data-testid="educator-first-name-input"
                    type="text"
                    value={newEducator.firstName}
                    onChange={(e) => setNewEducator({...newEducator, firstName: e.target.value})}
                    required
                    disabled={!!newEducator.accountHolderId}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Last Name *
                  </label>
                  <input
                    id="educator-last-name-input"
                    data-testid="educator-last-name-input"
                    type="text"
                    value={newEducator.lastName}
                    onChange={(e) => setNewEducator({...newEducator, lastName: e.target.value})}
                    required
                    disabled={!!newEducator.accountHolderId}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Email *
                  </label>
                  <input
                    id="educator-email-input"
                    data-testid="educator-email-input"
                    type="email"
                    value={newEducator.email}
                    onChange={(e) => setNewEducator({...newEducator, email: e.target.value})}
                    required
                    disabled={!!newEducator.accountHolderId}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Phone
                  </label>
                  <input
                    id="educator-phone-input"
                    data-testid="educator-phone-input"
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
                    id="educator-department-input"
                    data-testid="educator-department-input"
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

              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Bio
                </label>
                <textarea
                  id="educator-bio-input"
                  data-testid="educator-bio-input"
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
                  id="cancel-educator-btn"
                  data-testid="cancel-educator-btn"
                  onClick={() => {
                    setShowAddForm(false);
                    resetInviteForm();
                  }}
                  className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  id="save-educator-btn"
                  data-testid="save-educator-btn"
                  className="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600"
                >
                  Invite Educator
                </button>
              </div>
            </form>
          </div>
        )}

        {/* Educators List */}
        <div className="bg-white shadow overflow-hidden sm:rounded-md">
          <ul className="divide-y divide-gray-200">
            {educators.map((educator) => (
              <li key={educator.id} data-testid={`educator-${educator.fullName.replace(/\s+/g, '-').toLowerCase()}`}>
                <div className="px-4 py-4 sm:px-6">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <div>
                        <h3 className="text-lg font-medium text-gray-900">
                          {educator.fullName}
                          {educator.isDeleted && (
                            <span className="ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-200 text-gray-700">
                              Archived
                            </span>
                          )}
                          {!educator.isDeleted && !educator.isActive && (
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
                        </div>
                      </div>
                    </div>
                    {isAdmin && !educator.isDeleted && (
                      <div className="flex space-x-2">
                        <button
                          data-testid={`edit-educator-btn-${educator.id}`}
                          onClick={() => handleEditClick(educator)}
                          className="text-blue-600 hover:text-blue-900 text-sm font-medium"
                        >
                          Edit
                        </button>
                        {educator.isActive ? (
                          <button
                            onClick={() => handleToggleActive(educator.id, true)}
                            className="text-yellow-600 hover:text-yellow-900 text-sm font-medium"
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
                        <button
                          data-testid={`delete-educator-btn-${educator.id}`}
                          onClick={() => handleDeleteClick(educator.id)}
                          className="text-red-600 hover:text-red-900 text-sm font-medium"
                        >
                          Delete
                        </button>
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
              No educators found. {isAdmin && 'Click "Add Educator" to invite one.'}
            </div>
          )}
        </div>
      </main>

      {/* Edit Educator Modal */}
      {editingEducator && editForm && (
        <div className="fixed inset-0 bg-black bg-opacity-40 flex items-center justify-center z-50" data-testid="edit-educator-modal">
          <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-lg">
            <h2 className="text-xl font-semibold mb-4">Edit Educator</h2>
            <form onSubmit={handleEditSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">First Name *</label>
                  <input
                    data-testid="edit-first-name-input"
                    type="text"
                    value={editForm.firstName}
                    onChange={(e) => setEditForm({ ...editForm, firstName: e.target.value })}
                    required
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Last Name *</label>
                  <input
                    data-testid="edit-last-name-input"
                    type="text"
                    value={editForm.lastName}
                    onChange={(e) => setEditForm({ ...editForm, lastName: e.target.value })}
                    required
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Email</label>
                  <input
                    data-testid="edit-email-input"
                    type="email"
                    value={editForm.email || ''}
                    onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Phone</label>
                  <input
                    data-testid="edit-phone-input"
                    type="tel"
                    value={editForm.phone || ''}
                    onChange={(e) => setEditForm({ ...editForm, phone: e.target.value })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Department</label>
                  <input
                    data-testid="edit-department-input"
                    type="text"
                    value={editForm.educatorInfo?.department || ''}
                    onChange={(e) => setEditForm({ ...editForm, educatorInfo: { ...editForm.educatorInfo!, department: e.target.value } })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Bio</label>
                <textarea
                  data-testid="edit-bio-input"
                  value={editForm.educatorInfo?.bio || ''}
                  onChange={(e) => setEditForm({ ...editForm, educatorInfo: { ...editForm.educatorInfo!, bio: e.target.value } })}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>
              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  data-testid="cancel-edit-btn"
                  onClick={() => { setEditingEducator(null); setEditForm(null); }}
                  className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  data-testid="save-edit-btn"
                  className="px-4 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600"
                >
                  Save Changes
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Confirm Dialog */}
      {deleteConfirmId && (
        <div className="fixed inset-0 bg-black bg-opacity-40 flex items-center justify-center z-50" data-testid="delete-confirm-dialog">
          <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-sm">
            <h2 className="text-lg font-semibold mb-2">Delete Educator?</h2>
            <p className="text-sm text-gray-600 mb-4">
              If this educator has grade history, they will be archived rather than permanently removed.
            </p>
            <div className="flex justify-end space-x-2">
              <button
                data-testid="cancel-delete-btn"
                onClick={() => setDeleteConfirmId(null)}
                className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                data-testid="confirm-delete-btn"
                onClick={handleDeleteConfirm}
                className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
    </ProtectedRoute>
  );
};

export default EducatorsPage;
