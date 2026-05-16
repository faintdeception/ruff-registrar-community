import Image from 'next/image';
import Link from 'next/link';
import { useAuth } from '@/lib/auth';
import { useTenantExperience } from '@/lib/tenant-experience';
import { buildTenantPath } from '@/lib/runtime-env';
import { useRouter } from 'next/router';
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
  const { branding } = useTenantExperience();
  const router = useRouter();
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const settingsRef = useRef<HTMLDivElement>(null);
  const tenantPath = (path: string) => buildTenantPath(path, router.asPath);

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
  const brandName = branding?.displayName || 'Student Registrar';
  const footerText = branding?.footerText || 'Built for homeschool co-ops.';
  const poweredByHidden = branding?.hidePoweredBy ?? false;
  const primaryColor = branding?.primaryColor || '#3B82F6';

  useEffect(() => {
    const root = document.documentElement;
    root.style.setProperty('--tenant-primary-color', branding?.primaryColor || '#3B82F6');
    root.style.setProperty('--tenant-secondary-color', branding?.secondaryColor || '#10B981');

    return () => {
      root.style.setProperty('--tenant-primary-color', '#3B82F6');
      root.style.setProperty('--tenant-secondary-color', '#10B981');
    };
  }, [branding?.primaryColor, branding?.secondaryColor]);

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div className="flex items-center">
              <Link href={tenantPath('/')} className="flex items-center">
                {branding?.logoBase64 && branding?.logoMimeType ? (
                  <Image
                    src={`data:${branding.logoMimeType};base64,${branding.logoBase64}`}
                    alt={brandName}
                    className="h-10 w-10 rounded object-cover"
                    width={40}
                    height={40}
                    unoptimized
                  />
                ) : (
                  <AcademicCapIcon className="h-8 w-8" style={{ color: primaryColor }} />
                )}
                <h1 className="ml-2 text-2xl font-bold text-gray-900" style={{ color: primaryColor }}>
                  {brandName}
                </h1>
              </Link>
            </div>
            <div className="flex items-center space-x-4">
              <nav className="flex space-x-4" data-testid="main-navigation">
                {/* Public navigation (always visible) */}
                <Link
                  href={tenantPath('/courses')}
                  className="text-gray-600 hover:text-primary-600"
                  data-testid="nav-courses"
                  data-nav-item="courses"
                >
                  Courses
                </Link>
                <Link
                  href={tenantPath('/educators')}
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
                      href={tenantPath('/account-holder')}
                      className="text-gray-600 hover:text-primary-600"
                      data-testid="nav-account"
                      data-nav-item="account"
                    >
                      Account
                    </Link>
                    {user.roles.includes('Administrator') && (
                      <Link
                        href={tenantPath('/students')}
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
                        href={tenantPath('/semesters')}
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
                        href={tenantPath('/rooms')}
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
                            href={tenantPath('/settings/profile')}
                            data-testid="settings-profile"
                            className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                            onClick={() => setIsSettingsOpen(false)}
                          >
                            Profile
                          </Link>

                          {/* Manage Members - Admin only */}
                          {isAdmin && (
                            <Link
                              href={tenantPath('/settings/manage-members')}
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
                              href={tenantPath('/settings/system')}
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
                    href={tenantPath('/login')}
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
      <main className="flex-1">{children}</main>

      <footer className="border-t border-gray-200 bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 flex flex-col gap-2 text-sm text-gray-600 md:flex-row md:items-center md:justify-between">
          <div>{footerText}</div>
          {!poweredByHidden && (
            <div>
              Powered by <span style={{ color: primaryColor, fontWeight: 600 }}>Ruff Registrar</span>
            </div>
          )}
        </div>
      </footer>

      {branding?.sanitizedCustomCss && (
        <style dangerouslySetInnerHTML={{ __html: branding.sanitizedCustomCss }} />
      )}
    </div>
  );
}
