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

interface TenantPaymentConnectStatus {
  isSaaSMode: boolean;
  hasPaymentFeatures: boolean;
  platformStripeConfigured: boolean;
  isAvailable: boolean;
  isConnected: boolean;
  stripeConnectAccountId?: string | null;
  detailsSubmitted: boolean;
  chargesEnabled: boolean;
  payoutsEnabled: boolean;
  onboardingCompletedAtUtc?: string | null;
  unavailableReason?: string | null;
}

interface TenantPaymentConnectOnboardingLink {
  url: string;
  expiresAtUtc?: string | null;
}

interface TenantHomeContent {
  welcomeTitle: string;
  welcomeBlurb: string;
  hasCustomWelcomeTitle: boolean;
  hasCustomWelcomeBlurb: boolean;
}

export default function SystemSettings() {
  const { user } = useAuth();
  const [billing, setBilling] = useState<TenantBillingStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [connectSubmitting, setConnectSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [connectStatus, setConnectStatus] = useState<TenantPaymentConnectStatus | null>(null);
  const [homeContent, setHomeContent] = useState<TenantHomeContent | null>(null);
  const [welcomeTitleInput, setWelcomeTitleInput] = useState('');
  const [welcomeBlurbInput, setWelcomeBlurbInput] = useState('');

  const isAdmin = !!user?.roles.includes('Administrator');

  useEffect(() => {
    if (!isAdmin) {
      setLoading(false);
      return;
    }

    void fetchSettingsData();
  }, [isAdmin]);

  const fetchSettingsData = async () => {
    setLoading(true);
    await Promise.all([fetchBilling(), fetchConnectStatus(), fetchHomeContent()]);
    setLoading(false);
  };

  const fetchHomeContent = async () => {
    try {
      const response = await apiClient.get('/api/tenant-home-content');
      if (!response.ok) {
        throw new Error('Failed to load home content settings');
      }

      const data = await response.json() as TenantHomeContent;
      setHomeContent(data);
      setWelcomeTitleInput(data.hasCustomWelcomeTitle ? data.welcomeTitle : '');
      setWelcomeBlurbInput(data.hasCustomWelcomeBlurb ? data.welcomeBlurb : '');
    } catch (err) {
      console.error('Error loading home content settings:', err);
      setHomeContent(null);
    }
  };

  const fetchBilling = async () => {
    try {
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
    }
  };

  const fetchConnectStatus = async () => {
    try {
      const response = await apiClient.get('/api/tenant-payment-connect/status');
      if (!response.ok) {
        throw new Error('Failed to load Stripe Connect status');
      }

      const data = await response.json() as TenantPaymentConnectStatus;
      setConnectStatus(data);
    } catch (err) {
      console.error('Error loading Stripe Connect status:', err);
      setConnectStatus(null);
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
      await fetchSettingsData();
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
      await fetchSettingsData();
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to undo scheduled cancellation';
      setError(message);
      console.error('Error undoing scheduled cancellation:', err);
    } finally {
      setSubmitting(false);
    }
  };

  const handleConnectStripe = async () => {
    if (connectSubmitting) {
      return;
    }

    try {
      setConnectSubmitting(true);
      setError(null);

      const response = await apiClient.post('/api/tenant-payment-connect/onboarding-link');
      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Failed to start Stripe Connect onboarding');
      }

      const data = await response.json() as TenantPaymentConnectOnboardingLink;
      window.location.href = data.url;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to start Stripe Connect onboarding';
      setError(message);
      setConnectSubmitting(false);
    }
  };

  const handleRefreshConnectStatus = async () => {
    if (connectSubmitting) {
      return;
    }

    try {
      setConnectSubmitting(true);
      const response = await apiClient.post('/api/tenant-payment-connect/status/refresh');
      if (!response.ok) {
        throw new Error('Failed to refresh Stripe Connect status');
      }

      const data = await response.json() as TenantPaymentConnectStatus;
      setConnectStatus(data);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to refresh Stripe Connect status';
      setError(message);
    } finally {
      setConnectSubmitting(false);
    }
  };

  const handleSaveHomeContent = async () => {
    try {
      setSubmitting(true);
      setError(null);
      setSuccessMessage(null);

      const response = await apiClient.put('/api/tenant-home-content', {
        welcomeTitle: welcomeTitleInput,
        welcomeBlurb: welcomeBlurbInput,
      });

      if (!response.ok) {
        const message = await response.text();
        throw new Error(message || 'Failed to save home content settings');
      }

      const data = await response.json() as TenantHomeContent;
      setHomeContent(data);
      setWelcomeTitleInput(data.hasCustomWelcomeTitle ? data.welcomeTitle : '');
      setWelcomeBlurbInput(data.hasCustomWelcomeBlurb ? data.welcomeBlurb : '');
      setSuccessMessage('Landing page welcome content updated.');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to save home content settings';
      setError(message);
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

        <section className="rounded-xl border border-slate-200 bg-white shadow-sm" data-testid="home-content-settings-card">
          <div className="border-b border-slate-200 px-6 py-5">
            <h2 className="text-xl font-semibold text-slate-900">Home Landing Content</h2>
            <p className="mt-2 text-sm text-slate-600">
              Customize the dashboard welcome message shown to your organization after sign-in.
            </p>
          </div>

          <div className="px-6 py-6 space-y-4">
            <div>
              <label htmlFor="welcome-title" className="block text-sm font-medium text-slate-700">
                Welcome title
              </label>
              <input
                id="welcome-title"
                type="text"
                maxLength={120}
                value={welcomeTitleInput}
                onChange={(event) => setWelcomeTitleInput(event.target.value)}
                className="mt-2 block w-full rounded-md border border-slate-300 px-3 py-2 text-slate-900 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-200"
                placeholder={homeContent?.welcomeTitle ?? 'Welcome to Student Registrar'}
                data-testid="welcome-title-input"
              />
              <p className="mt-1 text-xs text-slate-500">
                Leave blank to default to "Welcome to {'{'}organization name{'}'}".
              </p>
            </div>

            <div>
              <label htmlFor="welcome-blurb" className="block text-sm font-medium text-slate-700">
                Welcome blurb
              </label>
              <textarea
                id="welcome-blurb"
                maxLength={600}
                rows={4}
                value={welcomeBlurbInput}
                onChange={(event) => setWelcomeBlurbInput(event.target.value)}
                className="mt-2 block w-full rounded-md border border-slate-300 px-3 py-2 text-slate-900 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-200"
                placeholder={homeContent?.welcomeBlurb ?? 'A comprehensive homeschool management system designed to help you track students, courses, rooms, and educators with ease.'}
                data-testid="welcome-blurb-input"
              />
              <p className="mt-1 text-xs text-slate-500">
                Leave blank to use the default blurb.
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={handleSaveHomeContent}
                disabled={submitting}
                className="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-primary-700 disabled:cursor-not-allowed disabled:bg-primary-300"
                data-testid="save-home-content-button"
              >
                {submitting ? 'Saving...' : 'Save Home Content'}
              </button>
            </div>
          </div>
        </section>

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
                {billing?.unavailableReason || 'Billing management is not available for this organization.'}
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

        <section className="rounded-xl border border-slate-200 bg-white shadow-sm" data-testid="tenant-payment-connect-card">
          <div className="border-b border-slate-200 px-6 py-5">
            <div className="flex items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-3">
                  <CreditCardIcon className="h-6 w-6 text-slate-700" />
                  <h2 className="text-xl font-semibold text-slate-900">Tenant Stripe Connect</h2>
                </div>
                <p className="mt-2 text-sm text-slate-600">
                  Connect your co-op Stripe account so family payments can settle directly to your organization.
                </p>
              </div>

              {connectStatus?.isConnected ? (
                <span className="inline-flex items-center rounded-full bg-blue-100 px-3 py-1 text-sm font-medium text-blue-800">
                  Connected
                </span>
              ) : (
                <span className="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-800">
                  Not connected
                </span>
              )}
            </div>
          </div>

          <div className="px-6 py-6 space-y-6">
            {!connectStatus?.isAvailable && (
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-700" data-testid="connect-unavailable-message">
                {connectStatus?.unavailableReason ?? 'Stripe Connect is not available for this tenant.'}
              </div>
            )}

            {connectStatus?.isAvailable && (
              <>
                <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
                  <StatusItem label="Account" value={connectStatus.stripeConnectAccountId ?? 'Not connected'} />
                  <StatusItem label="Details submitted" value={connectStatus.detailsSubmitted ? 'Yes' : 'No'} />
                  <StatusItem label="Charges enabled" value={connectStatus.chargesEnabled ? 'Yes' : 'No'} />
                  <StatusItem label="Payouts enabled" value={connectStatus.payoutsEnabled ? 'Yes' : 'No'} />
                </div>

                <div className="flex flex-wrap items-center gap-3">
                  <button
                    type="button"
                    onClick={handleConnectStripe}
                    disabled={!connectStatus.isAvailable || connectSubmitting}
                    className="inline-flex items-center rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:bg-indigo-300"
                    data-testid="connect-stripe-button"
                  >
                    {connectSubmitting
                      ? 'Working...'
                      : connectStatus.isConnected
                        ? 'Update Stripe Connection'
                        : 'Connect Stripe Account'}
                  </button>

                  <button
                    type="button"
                    onClick={handleRefreshConnectStatus}
                    disabled={connectSubmitting}
                    className="inline-flex items-center rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 shadow-sm transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                    data-testid="refresh-connect-status-button"
                  >
                    Refresh Status
                  </button>
                </div>
              </>
            )}
          </div>
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
