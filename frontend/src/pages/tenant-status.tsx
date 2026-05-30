import Link from 'next/link';
import { useRouter } from 'next/router';
import { ExclamationTriangleIcon, LifebuoyIcon, NoSymbolIcon } from '@heroicons/react/24/outline';
import { useTenantPath } from '@/lib/tenant-path';
import { extractTenantSlugFromPath } from '@/lib/tenant-routing';
import { getPortalBaseUrl } from '@/lib/runtime-env';

type TenantStatusConfig = {
  title: string;
  description: string;
  tone: 'amber' | 'red' | 'slate';
  canRecoverBilling: boolean;
};

const statusMap: Record<string, TenantStatusConfig> = {
  suspended: {
    title: 'Organization Access Is Suspended',
    description: 'This organization is currently suspended. Access usually returns after billing or account issues are resolved.',
    tone: 'amber',
    canRecoverBilling: true,
  },
  'access-ended': {
    title: 'Organization Access Has Ended',
    description: 'The current billing period has ended and this organization is no longer active in the tenant app.',
    tone: 'amber',
    canRecoverBilling: true,
  },
  'billing-hold': {
    title: 'Billing Must Be Restored',
    description: 'This organization is on billing hold. Access is blocked until billing is restored.',
    tone: 'amber',
    canRecoverBilling: true,
  },
  'pending-deletion': {
    title: 'Organization Is Pending Deletion',
    description: 'This organization is currently queued for final deletion. Tenant access is unavailable during that process.',
    tone: 'red',
    canRecoverBilling: false,
  },
  'deletion-failed': {
    title: 'Organization Is Awaiting Operator Review',
    description: 'A deletion or offboarding step failed and access is blocked until an administrator resolves it.',
    tone: 'red',
    canRecoverBilling: false,
  },
  deleted: {
    title: 'Organization Has Been Deleted',
    description: 'This organization is no longer available in Ruff Registrar.',
    tone: 'slate',
    canRecoverBilling: false,
  },
  'subscription-cancelled': {
    title: 'Subscription Has Been Cancelled',
    description: 'The organization subscription has been cancelled and access is no longer available from the tenant app.',
    tone: 'slate',
    canRecoverBilling: false,
  },
  inactive: {
    title: 'Organization Access Is Inactive',
    description: 'This organization is currently inactive. An administrator will need to review the organization status before access can continue.',
    tone: 'slate',
    canRecoverBilling: false,
  },
};

export default function TenantStatusPage() {
  const router = useRouter();
  const tenantPath = useTenantPath();

  const status = typeof router.query.status === 'string' ? router.query.status : 'inactive';
  const message = typeof router.query.message === 'string' ? router.query.message : null;
  const recover = router.query.recover === 'true';
  const config = statusMap[status] ?? statusMap.inactive;
  const showRecoveryHint = recover || config.canRecoverBilling;
  const portalRecoveryUrl = showRecoveryHint
    ? buildBillingRecoveryUrl(extractTenantSlugFromPath(router.asPath), getPortalBaseUrl())
    : null;

  const toneClasses = {
    amber: 'border-amber-200 bg-amber-50 text-amber-950',
    red: 'border-red-200 bg-red-50 text-red-950',
    slate: 'border-slate-200 bg-slate-50 text-slate-950',
  }[config.tone];

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col justify-center py-12 sm:px-6 lg:px-8">
      <div className="sm:mx-auto sm:w-full sm:max-w-2xl px-4">
        <div className="flex justify-center">
          {config.tone === 'red' ? (
            <NoSymbolIcon className="h-12 w-12 text-red-600" />
          ) : config.tone === 'amber' ? (
            <ExclamationTriangleIcon className="h-12 w-12 text-amber-600" />
          ) : (
            <LifebuoyIcon className="h-12 w-12 text-slate-700" />
          )}
        </div>
        <h1 className="mt-6 text-center text-3xl font-bold text-gray-900">{config.title}</h1>
        <p className="mt-3 text-center text-base text-gray-600">{config.description}</p>
      </div>

      <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-2xl px-4">
        <div className="bg-white py-8 px-6 shadow sm:rounded-lg sm:px-10 space-y-6">
          {message && (
            <div className={`rounded-lg border p-4 text-sm ${toneClasses}`}>
              {message}
            </div>
          )}

          {showRecoveryHint ? (
            <div className="rounded-lg border border-blue-200 bg-blue-50 p-4 text-sm text-blue-950">
              Billing recovery happens outside the tenant app. If your organization should still be active, contact your organization administrator and use the public billing recovery flow.
              {portalRecoveryUrl && (
                <div className="mt-4">
                  <a
                    href={portalRecoveryUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex justify-center rounded-md bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700"
                  >
                    Open Billing Recovery
                  </a>
                </div>
              )}
            </div>
          ) : (
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-800">
              If you believe this status is unexpected, contact your organization administrator or Ruff Registrar support for review.
            </div>
          )}

          <div className="flex flex-col sm:flex-row gap-3">
            <Link
              href={tenantPath('/login')}
              className="inline-flex justify-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-primary-700"
            >
              Return to Login
            </Link>
            <Link
              href={tenantPath('/')}
              className="inline-flex justify-center rounded-md border border-slate-300 px-4 py-2 text-sm font-medium text-slate-700 shadow-sm hover:bg-slate-50"
            >
              Back to Tenant Home
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

const buildBillingRecoveryUrl = (tenantSlug: string | null, portalBaseUrl: string | null): string | null => {
  if (!portalBaseUrl) {
    return null;
  }

  try {
    const url = new URL('/Billing/Reactivate', portalBaseUrl);
    if (tenantSlug) {
      url.searchParams.set('subdomain', tenantSlug);
    }

    return url.toString();
  } catch {
    return null;
  }
};