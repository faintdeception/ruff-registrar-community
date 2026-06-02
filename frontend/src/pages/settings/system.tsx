import { useEffect, useState } from 'react';
import {
  CalendarDaysIcon,
  CheckCircleIcon,
  CreditCardIcon,
  ExclamationTriangleIcon,
} from '@heroicons/react/24/outline';
import ProtectedRoute from '@/components/ProtectedRoute';
import { useAuth } from '@/lib/auth';
import apiClient from '@/lib/api-client';

interface TenantBillingStatus {
  isSaaSMode: boolean;
  canManageBilling: boolean;
  canUndoCancellation: boolean;
  unavailableReason?: string | null;
  subscriptionTier: string;
  subscriptionStatus: string;
  isComplimentary: boolean;
  hasStripeSubscription: boolean;
  cancelAtPeriodEnd: boolean;
  currentPeriodEndUtc?: string | null;
  offboardingStatus: string;
  accessEndsAtUtc?: string | null;
  deletionScheduledAtUtc?: string | null;
}

interface TenantBillingCancellation {
  subdomain: string;
  accessEndsAtUtc?: string | null;
  alreadyScheduled: boolean;
  cancellationScheduled: boolean;
  message: string;
}

export default function SystemSettings() {
  const { user } = useAuth();
  const [billing, setBilling] = useState<TenantBillingStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const isAdmin = !!user?.roles.includes('Administrator');

  useEffect(() => {
    if (!isAdmin) {
      setLoading(false);
      return;
    }

    void fetchBilling();
  }, [isAdmin]);

  const fetchBilling = async () => {
    try {
      setLoading(true);
      setError(null);

      const response = await apiClient.get('/api/tenant-billing');
      if (!response.ok) {
        throw new Error('Failed to load billing settings');
      }

      const data = await response.json() as TenantBillingStatus;
      setBilling(data);
    } catch (err) {
      setError('Failed to load billing settings');
      console.error('Error loading billing settings:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleScheduleCancellation = async () => {
    if (!billing?.canManageBilling || submitting) {
      return;
    }

    const confirmed = window.confirm('Schedule cancellation at the end of the current billing period?');
    if (!confirmed) {
      return;
    }

    try {
      setSubmitting(true);
      setError(null);
      setSuccessMessage(null);

      const response = await apiClient.post('/api/tenant-billing/cancel-at-period-end');
      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Failed to schedule cancellation');
      }

      const data = await response.json() as TenantBillingCancellation;
      setSuccessMessage(data.message);
      await fetchBilling();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to schedule cancellation';
      setError(message);
      console.error('Error scheduling cancellation:', err);
    } finally {
      setSubmitting(false);
    }
  };

  const handleUndoCancellation = async () => {
    if (!billing?.canUndoCancellation || submitting) {
      return;
    }

    const confirmed = window.confirm('Undo the scheduled cancellation and keep billing active?');
    if (!confirmed) {
      return;
    }

    try {
      setSubmitting(true);
      setError(null);
      setSuccessMessage(null);

      const response = await apiClient.post('/api/tenant-billing/undo-cancel-at-period-end');
      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Failed to undo scheduled cancellation');
      }

      const data = await response.json() as TenantBillingCancellation;
      setSuccessMessage(data.message);
      await fetchBilling();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to undo scheduled cancellation';
      setError(message);
      console.error('Error undoing scheduled cancellation:', err);
    } finally {
      setSubmitting(false);
    }
  };

  const formatDateTime = (value?: string | null) => {
    if (!value) {
      return 'Not scheduled';
    }

    return new Date(value).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      timeZoneName: 'short',
    });
  };

  const formatLabel = (value: string) => value.replace(/([a-z])([A-Z])/g, '$1 $2');

  if (!isAdmin) {
    return (
      <ProtectedRoute roles={['Administrator']}>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-red-800">
            Administrator access is required to manage system billing.
          </div>
        </div>
      </ProtectedRoute>
    );
  }

  if (loading) {
    return (
      <ProtectedRoute roles={['Administrator']}>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="text-center py-12">
            <div className="mx-auto h-12 w-12 animate-spin rounded-full border-b-2 border-primary-600" />
            <p className="mt-4 text-gray-600">Loading billing settings...</p>
          </div>
        </div>
      </ProtectedRoute>
    );
  }

  return (
    <ProtectedRoute roles={['Administrator']}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900" data-testid="system-settings-title">
            System Settings
          </h1>
          <p className="mt-2 text-gray-600">
            Manage subscription billing and review organization-wide system status.
          </p>
        </div>

        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-700">
            {error}
          </div>
        )}

        {successMessage && (
          <div className="rounded-lg border border-green-200 bg-green-50 p-4 text-green-700">
            {successMessage}
          </div>
        )}

        <section className="rounded-xl border border-slate-200 bg-white shadow-sm" data-testid="billing-management-card">
          <div className="border-b border-slate-200 px-6 py-5">
            <div className="flex items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-3">
                  <CreditCardIcon className="h-6 w-6 text-slate-700" />
                  <h2 className="text-xl font-semibold text-slate-900">Subscription Billing</h2>
                </div>
                <p className="mt-2 text-sm text-slate-600">
                  Cancel at period end from inside your tenant admin surface. Recovery remains available if billing later lapses.
                </p>
              </div>

              {billing?.cancelAtPeriodEnd ? (
                <span className="inline-flex items-center rounded-full bg-amber-100 px-3 py-1 text-sm font-medium text-amber-800">
                  Cancellation scheduled
                </span>
              ) : (
                <span className="inline-flex items-center rounded-full bg-emerald-100 px-3 py-1 text-sm font-medium text-emerald-800">
                  Active billing
                </span>
              )}
            </div>
          </div>

          <div className="px-6 py-6 space-y-6">
            {billing?.cancelAtPeriodEnd && (
              <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-amber-900">
                <div className="flex items-start gap-3">
                  <ExclamationTriangleIcon className="mt-0.5 h-5 w-5 flex-none" />
                  <div>
                    <p className="font-medium">Access ends at the close of the current billing period.</p>
                    <p className="mt-1 text-sm">
                      Access end: {formatDateTime(billing.accessEndsAtUtc)}. Deletion queue target: {formatDateTime(billing.deletionScheduledAtUtc)}.
                    </p>
                  </div>
                </div>
              </div>
            )}

            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
              <StatusItem label="Plan" value={billing?.subscriptionTier ?? 'Unknown'} />
              <StatusItem label="Subscription status" value={formatLabel(billing?.subscriptionStatus ?? 'Unknown')} />
              <StatusItem label="Current period end" value={formatDateTime(billing?.currentPeriodEndUtc)} icon={<CalendarDaysIcon className="h-5 w-5 text-slate-500" />} />
              <StatusItem label="Offboarding state" value={formatLabel(billing?.offboardingStatus ?? 'None')} icon={<CheckCircleIcon className="h-5 w-5 text-slate-500" />} />
            </div>

            {!billing?.canManageBilling && !billing?.canUndoCancellation && (
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-700" data-testid="billing-unavailable-message">
                {billing?.unavailableReason ?? 'Billing management is not available for this organization.'}
              </div>
            )}

            <div className="flex flex-wrap items-center gap-3">
              {billing?.canUndoCancellation ? (
                <button
                  type="button"
                  onClick={handleUndoCancellation}
                  disabled={!billing?.canUndoCancellation || submitting}
                  className="inline-flex items-center rounded-md bg-slate-700 px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:bg-slate-300"
                  data-testid="undo-cancellation-button"
                >
                  {submitting ? 'Updating...' : 'Keep Billing Active'}
                </button>
              ) : (
                <button
                  type="button"
                  onClick={handleScheduleCancellation}
                  disabled={!billing?.canManageBilling || submitting}
                  className="inline-flex items-center rounded-md bg-red-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-red-700 disabled:cursor-not-allowed disabled:bg-red-300"
                  data-testid="schedule-cancellation-button"
                >
                  {submitting ? 'Scheduling...' : 'Cancel At Period End'}
                </button>
              )}
              <p className="text-sm text-slate-600">
                {billing?.canUndoCancellation
                  ? 'This removes the pending offboarding schedule and keeps the organization on its active billing path.'
                  : 'This preserves access through the already-paid billing period, then moves the tenant into the existing offboarding pipeline.'}
              </p>
            </div>
          </div>
        </section>

        <section className="rounded-xl border border-blue-100 bg-blue-50 p-6">
          <h3 className="text-sm font-semibold uppercase tracking-wide text-blue-900">Next up</h3>
          <ul className="mt-3 space-y-2 text-sm text-blue-800">
            <li>Configure available payment types for educators</li>
            <li>Customize site logo and CSS for paid tiers</li>
            <li>Set membership fee defaults</li>
            <li>Manage broader system-wide policies</li>
          </ul>
        </section>
      </div>
    </ProtectedRoute>
  );
}

function StatusItem({
  label,
  value,
  icon,
}: {
  label: string;
  value: string;
  icon?: React.ReactNode;
}) {
  return (
    <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
      <div className="flex items-center gap-2 text-sm font-medium text-slate-600">
        {icon}
        <span>{label}</span>
      </div>
      <p className="mt-2 text-base font-semibold text-slate-900">{value}</p>
    </div>
  );
}
