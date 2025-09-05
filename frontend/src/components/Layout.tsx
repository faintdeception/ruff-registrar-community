import Link from 'next/link';
import { useAuth } from '@/lib/auth';
import { 
  AcademicCapIcon, 
  ArrowRightOnRectangleIcon,
  UserCircleIcon
} from '@heroicons/react/24/outline';

interface LayoutProps {
  children: React.ReactNode;
}

export default function Layout({ children }: LayoutProps) {
  const { user, logout } = useAuth();

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
                <Link 
                  href="/account-holder" 
                  className="text-gray-600 hover:text-primary-600"
                  data-testid="nav-account"
                  data-nav-item="account"
                >
                  Account
                </Link>
                {user?.roles.includes('Administrator') && (
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
                <Link 
                  href="/courses" 
                  className="text-gray-600 hover:text-primary-600"
                  data-testid="nav-courses"
                  data-nav-item="courses"
                >
                  Courses
                </Link>
                {user?.roles.includes('Administrator') && (
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
                {user?.roles.includes('Administrator') && (
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
                <Link 
                  href="/educators" 
                  className="text-gray-600 hover:text-primary-600"
                  data-testid="nav-educators"
                  data-nav-item="educators"
                >
                  Educators
                </Link>
              </nav>
              
              {/* User Menu */}
              <div className="flex items-center space-x-3" data-testid="user-menu">
                <div className="flex items-center space-x-2" data-testid="user-info">
                  <UserCircleIcon className="h-6 w-6 text-gray-400" />
                  <span className="text-sm text-gray-700" data-testid="user-name">
                    {user?.firstName} {user?.lastName}
                  </span>
                  <span className="text-xs text-gray-500" data-testid="user-roles">
                    ({user?.roles.join(', ')})
                  </span>
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
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      {children}
    </div>
  );
}
