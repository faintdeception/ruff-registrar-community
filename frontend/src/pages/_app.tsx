import type { AppProps } from 'next/app';
import { AuthProvider } from '@/lib/auth';
import { TenantExperienceProvider } from '@/lib/tenant-experience';
import '@/styles/globals.css';

export default function App({ Component, pageProps }: AppProps) {
  return (
    <AuthProvider>
      <TenantExperienceProvider>
        <Component {...pageProps} />
      </TenantExperienceProvider>
    </AuthProvider>
  );
}
