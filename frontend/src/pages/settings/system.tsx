import api from '@/lib/api';
import ProtectedRoute from '@/components/ProtectedRoute';
import { useTenantExperience, type BrandingSettings } from '@/lib/tenant-experience';
import { ChangeEvent, FormEvent, useEffect, useState } from 'react';
import { AxiosError } from 'axios';

interface BrandingFormState {
  displayName: string;
  logoBase64: string;
  logoMimeType: string;
  primaryColor: string;
  secondaryColor: string;
  footerText: string;
  hidePoweredBy: boolean;
  customCss: string;
}

const defaultBrandingState: BrandingFormState = {
  displayName: '',
  logoBase64: '',
  logoMimeType: '',
  primaryColor: '#3B82F6',
  secondaryColor: '#10B981',
  footerText: '',
  hidePoweredBy: false,
  customCss: '',
};

export default function SystemSettings() {
  const { features, branding, isLoadingFeatures, isLoadingBranding, refreshBranding, setBranding } = useTenantExperience();
  const [form, setForm] = useState<BrandingFormState>(defaultBrandingState);
  const [isSaving, setIsSaving] = useState(false);
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    setForm({
      displayName: branding?.displayName || '',
      logoBase64: branding?.logoBase64 || '',
      logoMimeType: branding?.logoMimeType || '',
      primaryColor: branding?.primaryColor || '#3B82F6',
      secondaryColor: branding?.secondaryColor || '#10B981',
      footerText: branding?.footerText || '',
      hidePoweredBy: branding?.hidePoweredBy || false,
      customCss: branding?.customCss || '',
    });
  }, [branding]);

  const handleFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    const dataUrl = await fileToDataUrl(file);
    const [, payload = ''] = dataUrl.split(',', 2);

    setForm((current) => ({
      ...current,
      logoBase64: payload,
      logoMimeType: file.type,
    }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    setStatusMessage(null);
    setErrorMessage(null);

    try {
      const response = await api.put<BrandingSettings>('/settings/branding', {
        displayName: emptyToNull(form.displayName),
        logoBase64: emptyToNull(form.logoBase64),
        logoMimeType: emptyToNull(form.logoMimeType),
        primaryColor: form.primaryColor,
        secondaryColor: form.secondaryColor,
        footerText: emptyToNull(form.footerText),
        hidePoweredBy: form.hidePoweredBy,
        customCss: emptyToNull(form.customCss),
      });

      setBranding(response.data);
      await refreshBranding();
      setStatusMessage('Branding settings saved.');
    } catch (error) {
      const message = error instanceof AxiosError
        ? (error.response?.data as { error?: string } | undefined)?.error
        : null;
      setErrorMessage(message || 'Unable to save branding settings right now.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <ProtectedRoute roles={['Administrator']}>
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900" data-testid="system-settings-title">
            System Settings
          </h1>
          <p className="mt-2 text-gray-600">
            Configure site-wide settings and preferences
          </p>
        </div>

        <div className="bg-white shadow-sm rounded-lg p-6">
          <div className="space-y-6">
            <div className="rounded-lg bg-gray-50 p-4">
              <h2 className="text-lg font-semibold text-gray-900 mb-2">Resolved Tenant Features</h2>
              {isLoadingFeatures ? (
                <p className="text-gray-600">Loading feature access…</p>
              ) : (
                <div className="flex flex-wrap gap-2">
                  {features?.enabledFeatures.length ? features.enabledFeatures.map((feature) => (
                    <span key={feature} className="rounded-full bg-slate-200 px-3 py-1 text-sm text-slate-800">
                      {feature}
                    </span>
                  )) : (
                    <span className="text-sm text-gray-600">No paid entitlements enabled.</span>
                  )}
                </div>
              )}
            </div>

            {!features?.hasBranding ? (
              <div className="rounded-lg border border-amber-200 bg-amber-50 p-6">
                <h2 className="text-xl font-semibold text-amber-900 mb-2">Enterprise Branding Locked</h2>
                <p className="text-amber-900/80">
                  Custom branding is available when the tenant has the <strong>branding</strong> entitlement.
                  This is normally included in the Enterprise tier or granted explicitly via an override.
                </p>
              </div>
            ) : (
              <form className="space-y-6" onSubmit={handleSubmit}>
                <div>
                  <h2 className="text-xl font-semibold text-gray-900 mb-1">Branding</h2>
                  <p className="text-gray-600">Configure the tenant app shell without editing shared code.</p>
                </div>

                {statusMessage && <div className="rounded-md bg-green-50 p-3 text-sm text-green-800">{statusMessage}</div>}
                {errorMessage && <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">{errorMessage}</div>}

                <div className="grid gap-6 md:grid-cols-2">
                  <div>
                    <label className="form-label" htmlFor="displayName">Display Name</label>
                    <input id="displayName" className="form-input" value={form.displayName} onChange={(event) => setForm((current) => ({ ...current, displayName: event.target.value }))} />
                  </div>
                  <div>
                    <label className="form-label" htmlFor="footerText">Footer Text</label>
                    <input id="footerText" className="form-input" value={form.footerText} onChange={(event) => setForm((current) => ({ ...current, footerText: event.target.value }))} />
                  </div>
                  <div>
                    <label className="form-label" htmlFor="primaryColor">Primary Color</label>
                    <input id="primaryColor" type="color" className="mt-1 block h-11 w-full rounded-md border border-gray-300" value={form.primaryColor} onChange={(event) => setForm((current) => ({ ...current, primaryColor: event.target.value }))} />
                  </div>
                  <div>
                    <label className="form-label" htmlFor="secondaryColor">Secondary Color</label>
                    <input id="secondaryColor" type="color" className="mt-1 block h-11 w-full rounded-md border border-gray-300" value={form.secondaryColor} onChange={(event) => setForm((current) => ({ ...current, secondaryColor: event.target.value }))} />
                  </div>
                </div>

                <div>
                  <label className="form-label" htmlFor="logoUpload">Logo</label>
                  <input id="logoUpload" type="file" accept="image/png,image/jpeg,image/svg+xml,image/webp" className="mt-1 block w-full text-sm text-gray-700" onChange={handleFileChange} />
                  {form.logoMimeType && <p className="mt-2 text-sm text-gray-500">Current logo MIME type: {form.logoMimeType}</p>}
                </div>

                <div>
                  <label className="form-label" htmlFor="customCss">Custom CSS</label>
                  <textarea id="customCss" className="form-input min-h-40" value={form.customCss} onChange={(event) => setForm((current) => ({ ...current, customCss: event.target.value }))} />
                  {isLoadingBranding ? (
                    <p className="mt-2 text-sm text-gray-500">Loading branding preview…</p>
                  ) : branding?.sanitizedCustomCss ? (
                    <p className="mt-2 text-sm text-gray-500">Sanitized CSS will be applied to the tenant layout after save.</p>
                  ) : null}
                </div>

                <label className="flex items-center gap-3 text-sm text-gray-700">
                  <input type="checkbox" checked={form.hidePoweredBy} onChange={(event) => setForm((current) => ({ ...current, hidePoweredBy: event.target.checked }))} />
                  Hide the default powered-by footer text
                </label>

                <div className="flex items-center gap-3">
                  <button type="submit" className="btn btn-primary" disabled={isSaving}>
                    {isSaving ? 'Saving…' : 'Save Branding'}
                  </button>
                  <button type="button" className="btn btn-secondary" onClick={() => setForm(defaultBrandingState)} disabled={isSaving}>
                    Clear Draft
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      </div>
    </ProtectedRoute>
  );
}

function emptyToNull(value: string) {
  return value.trim() ? value : null;
}

function fileToDataUrl(file: File) {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(new Error('Unable to read logo file.'));
    reader.readAsDataURL(file);
  });
}
