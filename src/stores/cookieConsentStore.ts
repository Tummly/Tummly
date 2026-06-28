import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";

import { setAnalyticsConsent } from "@/lib/analytics";

const COOKIE_CONSENT_KEY = "tummly-cookie-consent";

interface CookieConsentState {
  analytics: boolean | null;
  updatedAt: string | null;
  _hasHydrated: boolean;
  acceptAll: () => void;
  rejectNonEssential: () => void;
  setAnalytics: (enabled: boolean) => void;
  setHasHydrated: (value: boolean) => void;
}

function applyAnalyticsConsent(analytics: boolean | null): void {
  setAnalyticsConsent(analytics === true);
}

export const useCookieConsentStore = create<CookieConsentState>()(
  persist(
    (set) => ({
      analytics: null,
      updatedAt: null,
      _hasHydrated: false,
      acceptAll: () => {
        const updatedAt = new Date().toISOString();
        applyAnalyticsConsent(true);
        set({ analytics: true, updatedAt });
      },
      rejectNonEssential: () => {
        const updatedAt = new Date().toISOString();
        applyAnalyticsConsent(false);
        set({ analytics: false, updatedAt });
      },
      setAnalytics: (enabled) => {
        const updatedAt = new Date().toISOString();
        applyAnalyticsConsent(enabled);
        set({ analytics: enabled, updatedAt });
      },
      setHasHydrated: (value) => set({ _hasHydrated: value }),
    }),
    {
      name: COOKIE_CONSENT_KEY,
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        analytics: state.analytics,
        updatedAt: state.updatedAt,
      }),
      onRehydrateStorage: () => (state, error) => {
        if (error) {
          state?.setHasHydrated(true);
          return;
        }

        if (state?.analytics != null) {
          applyAnalyticsConsent(state.analytics);
        }

        state?.setHasHydrated(true);
      },
    },
  ),
);
