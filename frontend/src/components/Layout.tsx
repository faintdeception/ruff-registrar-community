import Link from 'next/link';
import { useAuth } from '@/lib/auth';
import { useState, useRef, useEffect } from 'react';
import {
  AcademicCapIcon,
  ArrowRightOnRectangleIcon,
  UserCircleIcon,
  Cog6ToothIcon
} from '@heroicons/react/24/outline';

interface LayoutProps {
  children: React.ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  const { user, logout } = useAuth();
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const settingsRef = useRef<HTMLDivElement>(null);

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (settingsRef.current && !settingsRef.current.contains(event.target as Node)) {
        setIsSettingsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const isAdmin = user?.roles.includes('Administrator');

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div className="flex items-center">
              <Link href="/" className="flex items-center">
                <AcademicCapIcon className="h-8 w-8 text-primary-600" />
                <h1 className="ml-2 text-2xl font-bold text-gray-900">
                  Student Registrar
                </h1>
              </Link>
            </div>
            <div className="flex items-center space-x-4">
              <nav className="flex space-x-4" data-testid="main-navigation">
                {/* Public navigation (always visible) */}
                <Link
                  href="/courses"
                  className="text-gray-600 hover:text-primary-600"
                  data-testid="nav-courses"
                  data-nav-item="courses"
                >
                  Courses
                </Link>
                <Link
                  href="/educators"
                  className="text-gray-600 hover:text-primary-600"
                  data-testid="nav-educators"
                  data-nav-item="educators"
                >
                  Educators
                </Link>
                {/* Authenticated-only navigation */}
                {user && (
                  <>
                    <Link
                      href="/account-holder"
                      className="text-gray-600 hover:text-primary-600"
                      data-testid="nav-account"
                      data-nav-item="account"
                    >
                      Account
                    </Link>
                    {user.roles.includes('Administrator') && (
                      <Link
                        href="/students"
                        className="text-gray-600 hover:text-primary-600"
                        data-testid="nav-students"
                        data-nav-item="students"
                        data-admin-only="true"
                      >
                        Students
                      </Link>
                    )}
                    {user.roles.includes('Administrator') && (
                      <Link
                        href="/semesters"
                        className="text-gray-600 hover:text-primary-600"
                        data-testid="nav-semesters"
                        data-nav-item="semesters"
                        data-admin-only="true"
                      >
                        Semesters
                      </Link>
                    )}
                    {user.roles.includes('Administrator') && (
                      <Link
                        href="/rooms"
                        className="text-gray-600 hover:text-primary-600"
                        data-testid="nav-rooms"
                        data-nav-item="rooms"
                        data-admin-only="true"
                      >
                        Rooms
                      </Link>
                    )}
                  </>
                )}
              </nav>

              {/* User Menu / Auth Controls */}
              {user ? (
                <div className="flex items-center space-x-3" data-testid="user-menu">
                  <div className="flex items-center space-x-2" data-testid="user-info">
                    <UserCircleIcon className="h-6 w-6 text-gray-400" />
                    <span className="text-sm text-gray-700" data-testid="user-name">
                      {user.firstName} {user.lastName}
                    </span>
                    {user.roles && user.roles.length > 0 && (
                      <span className="text-xs text-gray-500" data-testid="user-roles">
                        ({user.roles.join(', ')})
                      </span>
                    )}
                  </div>

                  {/* Settings Dropdown */}
                  <div className="relative" ref={settingsRef}>
                    <button
                      data-testid="settings-button"
                      onClick={() => setIsSettingsOpen(!isSettingsOpen)}
                      className="flex items-center space-x-1 text-gray-600 hover:text-primary-600"
                      aria-label="Settings"
                    >
                      <Cog6ToothIcon className="h-5 w-5" />
                    </button>

                    {isSettingsOpen && (
                      <div
                        data-testid="settings-dropdown"
                        className="absolute right-0 mt-2 w-56 bg-white rounded-md shadow-lg ring-1 ring-black ring-opacity-5 z-50"
                      >
                        <div className="py-1" role="menu">
                          {/* Profile - available to all authenticated users */}
                          <Link
                            href="/settings/profile"
                            data-testid="settings-profile"
                            className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                            onClick={() => setIsSettingsOpen(false)}
                          >
                            Profile
                          </Link>

                          {/* Manage Members - Admin only */}
                          {isAdmin && (
                            <Link
                              href="/settings/manage-members"
                              data-testid="settings-manage-members"
                              data-admin-only="true"
                              className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                              onClick={() => setIsSettingsOpen(false)}
                            >
                              Manage Members
                            </Link>
                          )}

                          {/* System Settings - Admin only */}
                          {isAdmin && (
                            <Link
                              href="/settings/system"
                              data-testid="settings-system"
                              data-admin-only="true"
                              className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                              onClick={() => setIsSettingsOpen(false)}
                            >
                              System Settings
                            </Link>
                          )}
                        </div>
                      </div>
                    )}
                  </div>

                  <button
                    id="logout-button"
                    data-testid="logout-button"
                    onClick={logout}
                    className="flex items-center space-x-1 text-gray-600 hover:text-red-600"
                  >
                    <ArrowRightOnRectangleIcon className="h-5 w-5" />
                    <span>Logout</span>
                  </button>
                </div>
              ) : (
                <div className="flex items-center space-x-4" data-testid="guest-menu">
                  <Link
                    href="/login"
                    className="text-gray-600 hover:text-primary-600 font-medium"
                    data-testid="login-link"
                  >
                    Login
                  </Link>
                </div>
              )}
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
  {children}
    </div>
  );
}
