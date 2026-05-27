import { useAuth } from '@/lib/auth';
import { buildTenantPath } from '@/lib/tenant-routing';
import { getTenantSlugFromPath } from '@/lib/runtime-env';
import { useRouter } from 'next/router';
import { useEffect } from 'react';
import Layout from './Layout';

interface ProtectedRouteProps {
  children: React.ReactNode;
  roles?: string[];
  showLayout?: boolean;
}

export default function ProtectedRoute({ children, roles, showLayout = true }: ProtectedRouteProps) {
  const { user, isLoading, isAuthenticated } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push(buildTenantPath('/login', getTenantSlugFromPath()));
    }
  }, [isLoading, isAuthenticated, router]);

  useEffect(() => {
    if (user && roles && roles.length > 0) {
      const hasRequiredRole = roles.some(role => user.roles.includes(role));
      if (!hasRequiredRole) {
        router.push(buildTenantPath('/unauthorized', getTenantSlugFromPath()));
      }
    }
  }, [user, roles, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  if (roles && roles.length > 0 && user) {
    const hasRequiredRole = roles.some(role => user.roles.includes(role));
    if (!hasRequiredRole) {
      return null;
    }
  }

  // For pages that shouldn't show layout (like login), return children directly
  if (!showLayout) {
    return <>{children}</>;
  }

  // For protected pages, wrap in layout
  return <Layout>{children}</Layout>;
}
