import type { AppProps } from 'next/app';
import Script from 'next/script';
import { AuthProvider } from '@/lib/auth';
import '@/styles/globals.css';

export default function App({ Component, pageProps }: AppProps) {
  return (
    <>
      <Script src="/env.js" strategy="beforeInteractive" />
      <AuthProvider>
        <Component {...pageProps} />
      </AuthProvider>
    </>
  );
}
