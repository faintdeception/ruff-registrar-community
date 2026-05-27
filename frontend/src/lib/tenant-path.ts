import { useRouter } from 'next/router';
import { buildTenantPath, extractTenantSlugFromPath } from './tenant-routing';

export const useTenantPath = () => {
  const router = useRouter();
  return (targetPath: string) => buildTenantPath(targetPath, extractTenantSlugFromPath(router.asPath));
};

export const toTenantPath = (currentPath: string, targetPath: string): string => {
  return buildTenantPath(targetPath, extractTenantSlugFromPath(currentPath));
};
