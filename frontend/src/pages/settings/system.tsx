import { useEffect, useState } from 'react';
import ProtectedRoute from '@/components/ProtectedRoute';
import apiClient from '@/lib/api-client';
import { useAuth } from '@/lib/auth';

interface PaymentOptionsResponse {
  isSupported: boolean;
  canManagePaymentOptions: boolean;
  subscriptionTier: string;
  upgradeMessage?: string | null;
  enableStripePayments: boolean;
  hasStripeAccountToken: boolean;
  stripeAccountTokenPreview?: string | null;
}

interface ApiErrorResponse {
  message?: string;
  debug?: string;
}

export default function SystemSettings() {
  const { user, isLoading: isAuthLoading, isAuthenticated } = useAuth();
  const [paymentOptions, setPaymentOptions] = useState<PaymentOptionsResponse | null>(null);
  const [enableStripePayments, setEnableStripePayments] = useState(false);
  const [stripeAccountToken, setStripeAccountToken] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  useEffect(() => {
    if (!isAuthLoading && isAuthenticated && user?.roles.includes('Administrator')) {
      void loadPaymentOptions();
    }
  }, [isAuthLoading, isAuthenticated, user]);

  const loadPaymentOptions = async () => {
    try {
      setLoading(true);
      setError(null);

      const response = await apiClient.get('/api/settings/payment-options');
      const payload = await response.json();

      if (!response.ok) {
        throw new Error(readApiError(payload) || 'Failed to load payment options');
      }

      setPaymentOptions(payload);
      setEnableStripePayments(payload.enableStripePayments);
      setStripeAccountToken('');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load payment options');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setSuccessMessage(null);

    try {
      const response = await apiClient.put('/api/settings/payment-options', {
        enableStripePayments,
        stripeAccountToken: stripeAccountToken.trim() || null,
      });
      const payload = await response.json();

      if (!response.ok) {
        throw new Error(readApiError(payload) || 'Failed to save payment options');
      }

      setPaymentOptions(payload);
      setEnableStripePayments(payload.enableStripePayments);
      setStripeAccountToken('');
      setSuccessMessage('Payment options updated.');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to save payment options');
    } finally {
      setSaving(false);
    }
  };

  const showTokenInput = enableStripePayments;

  return (
    <ProtectedRoute roles={['Administrator']}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900" data-testid="system-settings-title">
            System Settings
          </h1>
          <p className="mt-2 text-gray-600">
            Configure organization-wide billing and platform options.
          </p>
        </div>

        <div className="bg-white shadow-sm rounded-lg p-6" data-testid="payment-options-section">
          <div className="flex items-start justify-between gap-6 border-b border-gray-200 pb-6">
            <div>
              <h2 className="text-xl font-semibold text-gray-900">Payment Options</h2>
              <p className="mt-2 text-sm text-gray-600">
                Start with Stripe for hosted payment collection. Additional providers will be added later.
              </p>
            </div>
            {paymentOptions && (
              <span className="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-xs font-medium uppercase tracking-wide text-slate-700" data-testid="subscription-tier-badge">
                {paymentOptions.subscriptionTier}
              </span>
            )}
          </div>

          {loading || isAuthLoading ? (
            <div className="py-10 text-center text-gray-600" data-testid="payment-options-loading">
              Loading payment options...
            </div>
          ) : (
            <form className="space-y-6 pt-6" onSubmit={handleSave}>
              {error && (
                <div className="rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700" data-testid="payment-options-error">
                  {error}
                </div>
              )}

              {successMessage && (
                <div className="rounded-md border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-700" data-testid="payment-options-success">
                  {successMessage}
                </div>
              )}

              {paymentOptions && !paymentOptions.canManagePaymentOptions && paymentOptions.upgradeMessage && (
                <div className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800" data-testid="paid-tier-required-message">
                  {paymentOptions.upgradeMessage}
                </div>
              )}

              {paymentOptions && !paymentOptions.isSupported && (
                <div className="rounded-md border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-700">
                  Payment options are not available in the current environment.
                </div>
              )}

              <div className="rounded-lg border border-gray-200 p-5">
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <label htmlFor="enable-stripe-payments" className="text-base font-medium text-gray-900">
                      Enable Stripe Payments
                    </label>
                    <p className="mt-1 text-sm text-gray-600">
                      Allow your organization to accept Stripe-backed payments for courses and dues.
                    </p>
                  </div>
                  <input
                    id="enable-stripe-payments"
                    data-testid="enable-stripe-payments-toggle"
                    type="checkbox"
                    checked={enableStripePayments}
                    disabled={!paymentOptions?.canManagePaymentOptions || !paymentOptions?.isSupported || saving}
                    onChange={(event) => {
                      setEnableStripePayments(event.target.checked);
                      setSuccessMessage(null);
                    }}
                    className="mt-1 h-5 w-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                  />
                </div>

                {showTokenInput && (
                  <div className="mt-5 space-y-3">
                    <div>
                      <label htmlFor="stripe-account-token" className="block text-sm font-medium text-gray-700">
                        Stripe Account Token
                      </label>
                      <input
                        id="stripe-account-token"
                        data-testid="stripe-token-input"
                        type="text"
                        value={stripeAccountToken}
                        onChange={(event) => {
                          setStripeAccountToken(event.target.value);
                          setSuccessMessage(null);
                        }}
                        disabled={!paymentOptions?.canManagePaymentOptions || saving}
                        placeholder={paymentOptions?.hasStripeAccountToken ? 'Enter a new token to replace the stored value' : 'acct_... or your Stripe link token'}
                        className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500 disabled:bg-gray-100"
                      />
                    </div>

                    {paymentOptions?.hasStripeAccountToken && (
                      <p className="text-sm text-gray-600" data-testid="stripe-token-stored-indicator">
                        Stored token: {paymentOptions.stripeAccountTokenPreview}
                      </p>
                    )}

                    <p className="text-sm text-gray-500">
                      This token links Stripe payouts to your organization. Leave the field blank to keep the currently stored token.
                    </p>
                  </div>
                )}
              </div>

              <div className="flex items-center justify-between border-t border-gray-200 pt-6">
                <p className="text-sm text-gray-500">
                  Google Pay, Apple Pay, and PayPal will be added in later releases.
                </p>
                <button
                  type="submit"
                  data-testid="payment-options-save-button"
                  disabled={saving || !paymentOptions?.isSupported || !paymentOptions?.canManagePaymentOptions}
                  className="inline-flex items-center rounded-md bg-primary-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-primary-700 disabled:cursor-not-allowed disabled:bg-gray-300"
                >
                  {saving ? 'Saving...' : 'Save Payment Options'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </ProtectedRoute>
  );
}

function readApiError(payload: ApiErrorResponse | null | undefined): string | null {
  if (!payload) {
    return null;
  }

  const values = [payload.message, payload.debug].filter(
    (value): value is string => Boolean(value && value.trim())
  );

  return values.length > 0 ? values.join(' | ') : null;
}
