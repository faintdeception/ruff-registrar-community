import { useEffect, useState } from 'react';
import ProtectedRoute from '@/components/ProtectedRoute';
import { useAuth } from '@/lib/auth';
import apiClient from '@/lib/api-client';

interface UserProfile {
  phoneNumber?: string | null;
  bio?: string | null;
}

interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roleDisplay?: string;
  roles?: string[];
  profile?: UserProfile | null;
}

interface ProfileFormState {
  firstName: string;
  lastName: string;
  phoneNumber: string;
  bio: string;
}

export default function ProfileSettings() {
  const { user } = useAuth();
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [form, setForm] = useState<ProfileFormState>({
    firstName: '',
    lastName: '',
    phoneNumber: '',
    bio: '',
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!user) {
      return;
    }

    void fetchCurrentUser();
  }, [user]);

  const fetchCurrentUser = async () => {
    try {
      setLoading(true);
      setError(null);

      const response = await apiClient.get('/api/Users/me');
      if (!response.ok) {
        throw new Error('Failed to load profile settings');
      }

      const data = await response.json() as CurrentUser;
      setCurrentUser(data);
      setForm({
        firstName: data.firstName ?? '',
        lastName: data.lastName ?? '',
        phoneNumber: data.profile?.phoneNumber ?? '',
        bio: data.profile?.bio ?? '',
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load profile settings');
    } finally {
      setLoading(false);
    }
  };

  const handleFieldChange = (field: keyof ProfileFormState, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const handleSave = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!currentUser || saving) {
      return;
    }

    try {
      setSaving(true);
      setError(null);
      setSuccessMessage(null);

      const payload = {
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        profile: {
          phoneNumber: form.phoneNumber.trim() || null,
          bio: form.bio.trim() || null,
        },
      };

      const response = await apiClient.put(`/api/Users/${currentUser.id}`, payload);
      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Failed to save profile');
      }

      setCurrentUser((prev) => prev
        ? {
            ...prev,
            firstName: payload.firstName,
            lastName: payload.lastName,
            profile: {
              ...prev.profile,
              phoneNumber: payload.profile.phoneNumber,
              bio: payload.profile.bio,
            },
          }
        : prev);
      setSuccessMessage('Profile updated successfully.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save profile');
    } finally {
      setSaving(false);
    }
  };

  const roleLabel = currentUser?.roleDisplay ?? user?.roles?.[0] ?? 'Member';

  return (
    <ProtectedRoute>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900" data-testid="profile-settings-title">
            Profile Settings
          </h1>
          <p className="mt-2 text-gray-600">
            Manage your personal information.
          </p>
        </div>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-700" data-testid="profile-error-message">
            {error}
          </div>
        )}

        {successMessage && (
          <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-green-700" data-testid="profile-success-message">
            {successMessage}
          </div>
        )}

        {loading ? (
          <div className="bg-white shadow-sm rounded-lg p-6">
            <div className="text-center py-12">
              <div className="inline-block h-8 w-8 animate-spin rounded-full border-b-2 border-blue-600" />
              <p className="mt-4 text-gray-600">Loading profile...</p>
            </div>
          </div>
        ) : (
          <div className="bg-white shadow-sm rounded-lg p-6" data-testid="profile-settings-form-card">
            <form className="space-y-6" onSubmit={handleSave}>
              <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
                <div>
                  <label htmlFor="firstName" className="block text-sm font-medium text-gray-700">
                    First name
                  </label>
                  <input
                    id="firstName"
                    type="text"
                    value={form.firstName}
                    onChange={(event) => handleFieldChange('firstName', event.target.value)}
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                    maxLength={100}
                    required
                    data-testid="profile-first-name-input"
                  />
                </div>

                <div>
                  <label htmlFor="lastName" className="block text-sm font-medium text-gray-700">
                    Last name
                  </label>
                  <input
                    id="lastName"
                    type="text"
                    value={form.lastName}
                    onChange={(event) => handleFieldChange('lastName', event.target.value)}
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                    maxLength={100}
                    required
                    data-testid="profile-last-name-input"
                  />
                </div>

                <div>
                  <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                    Email
                  </label>
                  <input
                    id="email"
                    type="email"
                    value={currentUser?.email ?? user?.email ?? ''}
                    readOnly
                    className="mt-1 block w-full rounded-md border border-gray-200 bg-gray-100 px-3 py-2 text-sm text-gray-700"
                    data-testid="profile-email-input"
                  />
                  <p className="mt-1 text-xs text-gray-500">Email is managed by account identity settings.</p>
                </div>

                <div>
                  <label htmlFor="role" className="block text-sm font-medium text-gray-700">
                    Role
                  </label>
                  <input
                    id="role"
                    type="text"
                    value={roleLabel}
                    readOnly
                    className="mt-1 block w-full rounded-md border border-gray-200 bg-gray-100 px-3 py-2 text-sm text-gray-700"
                    data-testid="profile-role-input"
                  />
                </div>

                <div className="md:col-span-2">
                  <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700">
                    Phone number
                  </label>
                  <input
                    id="phoneNumber"
                    type="tel"
                    value={form.phoneNumber}
                    onChange={(event) => handleFieldChange('phoneNumber', event.target.value)}
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                    maxLength={30}
                    data-testid="profile-phone-input"
                  />
                </div>

                <div className="md:col-span-2">
                  <label htmlFor="bio" className="block text-sm font-medium text-gray-700">
                    Bio
                  </label>
                  <textarea
                    id="bio"
                    value={form.bio}
                    onChange={(event) => handleFieldChange('bio', event.target.value)}
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-900 shadow-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                    rows={4}
                    maxLength={1000}
                    data-testid="profile-bio-input"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end">
                <button
                  type="submit"
                  disabled={saving}
                  className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-blue-300"
                  data-testid="profile-save-button"
                >
                  {saving ? 'Saving...' : 'Save Changes'}
                </button>
              </div>
            </form>
          </div>
        )}
      </div>
    </ProtectedRoute>
  );
}
