import { useState, useEffect } from 'react';
import { useAuth } from '@/lib/auth';
import ProtectedRoute from '@/components/ProtectedRoute';
import apiClient from '@/lib/api-client';
import {
  BuildingOfficeIcon,
  PlusIcon,
  PencilIcon,
  TrashIcon,
  XCircleIcon,
  UsersIcon,
  InformationCircleIcon
} from '@heroicons/react/24/outline';

interface Room {
  id: string;
  name: string;
  capacity: number;
  notes?: string;
  roomType: RoomType;
  courseCount: number;
  createdAt: string;
  updatedAt: string;
}

enum RoomType {
  Classroom = 1,
  Lab = 2,
  Auditorium = 3,
  Library = 4,
  Gym = 5,
  Workshop = 6,
  Other = 7
}

interface CreateRoomDto {
  name: string;
  capacity: number;
  notes?: string;
  roomType: RoomType;
}

const getRoomTypeLabel = (roomType: RoomType): string => {
  switch (roomType) {
    case RoomType.Classroom: return 'Classroom';
    case RoomType.Lab: return 'Lab';
    case RoomType.Auditorium: return 'Auditorium';
    case RoomType.Library: return 'Library';
    case RoomType.Gym: return 'Gym';
    case RoomType.Workshop: return 'Workshop';
    case RoomType.Other: return 'Other';
    default: return 'Unknown';
  }
};

const getRoomTypeColor = (roomType: RoomType): string => {
  switch (roomType) {
    case RoomType.Classroom: return 'bg-blue-100 text-blue-800';
    case RoomType.Lab: return 'bg-purple-100 text-purple-800';
    case RoomType.Auditorium: return 'bg-red-100 text-red-800';
    case RoomType.Library: return 'bg-green-100 text-green-800';
    case RoomType.Gym: return 'bg-orange-100 text-orange-800';
    case RoomType.Workshop: return 'bg-yellow-100 text-yellow-800';
    case RoomType.Other: return 'bg-gray-100 text-gray-800';
    default: return 'bg-gray-100 text-gray-800';
  }
};

export default function RoomsPage() {
  const { user } = useAuth();
  const [rooms, setRooms] = useState<Room[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingRoom, setEditingRoom] = useState<Room | null>(null);
  const [formData, setFormData] = useState<CreateRoomDto>({
    name: '',
    capacity: 20,
    notes: '',
    roomType: RoomType.Classroom
  });
  const [submitting, setSubmitting] = useState(false);

  const isAdmin = user?.roles.includes('Administrator');

  useEffect(() => {
    if (isAdmin) {
      fetchRooms();
    }
  }, [isAdmin]);

  const fetchRooms = async () => {
    try {
      setLoading(true);

      const response = await apiClient.get('/api/rooms');

      if (!response.ok) {
        throw new Error('Failed to fetch rooms');
      }

      const data = await response.json();
      setRooms(data);
    } catch (err) {
      setError('Failed to fetch rooms');
      console.error('Error fetching rooms:', err);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({
      name: '',
      capacity: 20,
      notes: '',
      roomType: RoomType.Classroom
    });
    setEditingRoom(null);
  };

  const openCreateModal = () => {
    resetForm();
    setShowCreateModal(true);
  };

  const openEditModal = (room: Room) => {
    setFormData({
      name: room.name,
      capacity: room.capacity,
      notes: room.notes || '',
      roomType: room.roomType
    });
    setEditingRoom(room);
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
      // Validate capacity
      if (formData.capacity < 1 || formData.capacity > 1000) {
        throw new Error('Capacity must be between 1 and 1000');
      }

      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      let response;
      if (editingRoom) {
        response = await fetch(`/api/rooms/${editingRoom.id}`, {
          method: 'PUT',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(formData),
        });
      } else {
        response = await fetch('/api/rooms', {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(formData),
        });
      }

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to save room');
      }

      await fetchRooms();
      closeModal();
    } catch (err: any) {
      setError(err.message || 'Failed to save room');
      console.error('Error saving room:', err);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (roomId: string, roomName: string) => {
    if (!confirm(`Are you sure you want to delete the room "${roomName}"? This action cannot be undone.`)) {
      return;
    }

    try {
      const token = localStorage.getItem('accessToken');
      if (!token) {
        throw new Error('No authentication token found');
      }

      const response = await fetch(`/api/rooms/${roomId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || 'Failed to delete room');
      }

      await fetchRooms();
    } catch (err: any) {
      setError(err.message || 'Failed to delete room');
      console.error('Error deleting room:', err);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
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
            <p className="mt-4 text-gray-600">Loading rooms...</p>
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
                <BuildingOfficeIcon className="h-8 w-8 text-primary-600" />
                <h1 className="ml-3 text-2xl font-bold text-gray-900">Room Management</h1>
              </div>
              <button 
                id="create-room-btn"
                onClick={openCreateModal} 
                className="btn btn-primary"
              >
                <PlusIcon className="h-5 w-5" />
                Create Room
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

          {/* Rooms List */}
          {rooms.length === 0 ? (
            <div className="text-center py-12">
              <BuildingOfficeIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No rooms found</h3>
              <p className="text-gray-600 mb-6">
                Create your first room to start organizing courses by location.
              </p>
              <button 
                id="create-first-room-btn"
                onClick={openCreateModal} 
                className="btn btn-primary"
              >
                <PlusIcon className="h-5 w-5" />
                Create First Room
              </button>
            </div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
              {rooms.map((room) => (
                <div 
                  key={room.id} 
                  id={`room-card-${room.id}`}
                  data-testid={`room-${room.name.replace(/\s+/g, '-').toLowerCase()}`}
                  className="bg-white rounded-lg shadow hover:shadow-md transition-shadow"
                >
                  <div className="p-6">
                    {/* Header */}
                    <div className="flex items-start justify-between mb-4">
                      <div className="flex-1 min-w-0">
                        <h3 className="text-lg font-semibold text-gray-900 truncate">
                          {room.name}
                        </h3>
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium mt-1 ${getRoomTypeColor(room.roomType)}`}>
                          {getRoomTypeLabel(room.roomType)}
                        </span>
                      </div>
                      <div className="flex space-x-2 ml-4">
                        <button
                          id={`edit-room-${room.id}`}
                          onClick={() => openEditModal(room)}
                          className="text-gray-400 hover:text-gray-600"
                          title="Edit room"
                        >
                          <PencilIcon className="h-5 w-5" />
                        </button>
                        <button
                          id={`delete-room-${room.id}`}
                          onClick={() => handleDelete(room.id, room.name)}
                          className="text-gray-400 hover:text-red-600"
                          title="Delete room"
                        >
                          <TrashIcon className="h-5 w-5" />
                        </button>
                      </div>
                    </div>

                    {/* Details */}
                    <div className="space-y-3 mb-4">
                      <div className="flex items-center text-sm text-gray-600">
                        <UsersIcon className="h-4 w-4 mr-2" />
                        <span className="font-medium">Capacity:</span>
                        <span className="ml-1 text-gray-900">{room.capacity} people</span>
                      </div>
                      
                      {room.notes && (
                        <div className="flex items-start text-sm text-gray-600">
                          <InformationCircleIcon className="h-4 w-4 mr-2 mt-0.5 flex-shrink-0" />
                          <div>
                            <span className="font-medium">Notes:</span>
                            <p className="text-gray-900 mt-1">{room.notes}</p>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Stats */}
                    <div className="flex items-center justify-between pt-4 border-t border-gray-200">
                      <div className="flex items-center text-sm text-gray-600">
                        <span>{room.courseCount} course{room.courseCount !== 1 ? 's' : ''}</span>
                      </div>
                      <div className="text-xs text-gray-500">
                        Added {formatDate(room.createdAt)}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Create/Edit Modal */}
        {showCreateModal && (
          <div id="room-modal" className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50">
            <div className="relative top-20 mx-auto p-5 border w-full max-w-md shadow-lg rounded-md bg-white">
              <div className="mt-3">
                <h3 id="modal-title" className="text-lg font-medium text-gray-900 mb-4">
                  {editingRoom ? 'Edit Room' : 'Create New Room'}
                </h3>
                
                <form onSubmit={handleSubmit} className="space-y-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Room Name *
                    </label>
                    <input
                      id="room-name-input"
                      type="text"
                      required
                      value={formData.name}
                      onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                      className="form-input"
                      placeholder="e.g., Math Lab A"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Room Type *
                    </label>
                    <select
                      id="room-type-select"
                      required
                      value={formData.roomType}
                      onChange={(e) => setFormData({ ...formData, roomType: parseInt(e.target.value) as RoomType })}
                      className="form-select"
                    >
                      <option value={RoomType.Classroom}>Classroom</option>
                      <option value={RoomType.Lab}>Lab</option>
                      <option value={RoomType.Auditorium}>Auditorium</option>
                      <option value={RoomType.Library}>Library</option>
                      <option value={RoomType.Gym}>Gym</option>
                      <option value={RoomType.Workshop}>Workshop</option>
                      <option value={RoomType.Other}>Other</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Capacity *
                    </label>
                    <input
                      id="room-capacity-input"
                      type="number"
                      required
                      min="1"
                      max="1000"
                      value={formData.capacity}
                      onChange={(e) => setFormData({ ...formData, capacity: parseInt(e.target.value) || 0 })}
                      className="form-input"
                      placeholder="e.g., 25"
                    />
                  </div>
                  
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Notes
                    </label>
                    <textarea
                      id="room-notes-input"
                      value={formData.notes}
                      onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                      className="form-textarea"
                      rows={3}
                      placeholder="Optional notes about the room..."
                    />
                  </div>

                  {error && (
                    <div id="error-message" className="bg-red-50 border border-red-200 rounded-md p-3">
                      <p className="text-red-600 text-sm">{error}</p>
                    </div>
                  )}

                  <div className="flex justify-end space-x-3 pt-4">
                    <button
                      id="cancel-room-btn"
                      type="button"
                      onClick={closeModal}
                      disabled={submitting}
                      className="btn btn-secondary"
                    >
                      Cancel
                    </button>
                    <button
                      id="save-room-btn"
                      type="submit"
                      disabled={submitting}
                      className="btn btn-primary"
                    >
                      {submitting ? 'Saving...' : editingRoom ? 'Update Room' : 'Create Room'}
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
