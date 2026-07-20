import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/router';
import { getApiBaseUrl, getTenantSlugFromPath } from '@/lib/runtime-env';
import { buildTenantPath } from '@/lib/tenant-routing';

type Status = 'loading' | 'success' | 'error';

interface ConfirmEmailChangeResponse {
  email: string;
  message: string;
}

export default function ConfirmEmailChangePage() {
  const router = useRouter();
  const tenantSlug = getTenantSlugFromPath();
  const [status, setStatus] = useState<Status>('loading');
  const [message, setMessage] = useState('Confirming your new email address...');

  useEffect(() => {
    if (!router.isReady) {
      return;
    }

    const token = Array.isArray(router.query.token) ? router.query.token[0] : router.query.token;
    if (!token) {
      setStatus('error');
      setMessage('This email change link is missing a token.');
      return;
    }

    const confirmEmailChange = async () => {
      try {
        const headers: Record<string, string> = {
          'Content-Type': 'application/json',
        };

        if (tenantSlug) {
          headers['X-Tenant-Slug'] = tenantSlug;
        }

        const response = await fetch(`${getApiBaseUrl().replace(/\/$/, '')}/api/Users/confirm-email-change`, {
          method: 'POST',
          headers,
          body: JSON.stringify({ token }),
        });

        if (!response.ok) {
          const errorMessage = await response.text();
          throw new Error(errorMessage || 'Unable to confirm your email change.');
        }

        const payload = await response.json() as ConfirmEmailChangeResponse;
        setStatus('success');
        setMessage(payload.message || `Email address confirmed for ${payload.email}.`);
      } catch (error) {
        setStatus('error');
        setMessage(error instanceof Error ? error.message : 'Unable to confirm your email change.');
      }
    };

    void confirmEmailChange();
  }, [router.isReady, router.query.token, tenantSlug]);

  const loginPath = buildTenantPath('/login', tenantSlug);

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center px-4 py-12">
      <div className="w-full max-w-lg rounded-xl bg-white p-8 shadow-sm">
        <h1 className="text-2xl font-bold text-gray-900">Confirm Email Change</h1>
        <p className="mt-4 text-sm text-gray-700" data-testid="confirm-email-change-message">
          {message}
        </p>

        {status === 'loading' && (
          <div className="mt-6 flex items-center gap-3 text-sm text-gray-600">
            <div className="h-5 w-5 animate-spin rounded-full border-2 border-blue-600 border-b-transparent" />
            Processing confirmation link...
          </div>
        )}

        {status !== 'loading' && (
          <div className="mt-6">
            <Link
              href={loginPath}
              className="inline-flex items-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
            >
              Return to sign in
            </Link>
          </div>
        )}
      </div>
    </div>
  );
}