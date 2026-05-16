import React, { createContext, useCallback, useContext, useEffect, useState } from 'react';
import api from './api';
import { useAuth } from './auth';

export interface TenantFeatures {
  subscriptionTier: string;
  isSelfHostedMode: boolean;
  hasBranding: boolean;
  hasPayments: boolean;
  hasMembershipFees: boolean;
  hasPrioritySupport: boolean;
  enabledFeatures: string[];
}

export interface BrandingSettings {
  displayName: string | null;
  logoBase64: string | null;
  logoMimeType: string | null;
  primaryColor: string;
  secondaryColor: string;
  footerText: string | null;
  hidePoweredBy: boolean;
  customCss: string | null;
  sanitizedCustomCss: string;
}

interface TenantExperienceContextValue {
  features: TenantFeatures | null;
  branding: BrandingSettings | null;
  isLoadingFeatures: boolean;
  isLoadingBranding: boolean;
  refreshFeatures: () => Promise<void>;
  refreshBranding: () => Promise<void>;
  setBranding: (branding: BrandingSettings | null) => void;
}

const TenantExperienceContext = createContext<TenantExperienceContextValue | undefined>(undefined);

export function TenantExperienceProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading: isAuthLoading } = useAuth();
  const [features, setFeatures] = useState<TenantFeatures | null>(null);
  const [branding, setBranding] = useState<BrandingSettings | null>(null);
  const [isLoadingFeatures, setIsLoadingFeatures] = useState(true);
  const [isLoadingBranding, setIsLoadingBranding] = useState(false);

  const refreshFeatures = useCallback(async () => {
    if (!isAuthenticated) {
      setFeatures(null);
      setBranding(null);
      setIsLoadingFeatures(false);
      setIsLoadingBranding(false);
      return;
    }

    setIsLoadingFeatures(true);
    try {
      const response = await api.get<TenantFeatures>('/tenant/features');
      setFeatures(response.data);
    } finally {
      setIsLoadingFeatures(false);
    }
  }, [isAuthenticated]);

  const refreshBranding = useCallback(async () => {
    if (!isAuthenticated || !features?.hasBranding) {
      setBranding(null);
      setIsLoadingBranding(false);
      return;
    }

    setIsLoadingBranding(true);
    try {
      const response = await api.get<BrandingSettings>('/settings/branding');
      setBranding(response.data);
    } finally {
      setIsLoadingBranding(false);
    }
  }, [features?.hasBranding, isAuthenticated]);

  useEffect(() => {
    if (isAuthLoading) {
      return;
    }

    void refreshFeatures();
  }, [isAuthLoading, isAuthenticated, refreshFeatures]);

  useEffect(() => {
    if (isAuthLoading || isLoadingFeatures) {
      return;
    }

    void refreshBranding();
  }, [isAuthLoading, isLoadingFeatures, features?.hasBranding, isAuthenticated, refreshBranding]);

  return (
    <TenantExperienceContext.Provider value={{
      features,
      branding,
      isLoadingFeatures,
      isLoadingBranding,
      refreshFeatures,
      refreshBranding,
      setBranding,
    }}>
      {children}
    </TenantExperienceContext.Provider>
  );
}

export function useTenantExperience() {
  const context = useContext(TenantExperienceContext);
  if (!context) {
    throw new Error('useTenantExperience must be used within a TenantExperienceProvider');
  }

  return context;
}