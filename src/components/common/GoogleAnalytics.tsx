import { useEffect } from "react";
import { useLocation } from "react-router-dom";

import { setAnalyticsConsent, trackPageView } from "@/lib/analytics";
import { useCookieConsentStore } from "@/stores/cookieConsentStore";

function buildPagePath(pathname: string, search: string, hash: string): string {
  return `${pathname}${search}${hash}`;
}

export function GoogleAnalytics() {
  const location = useLocation();
  const analytics = useCookieConsentStore((state) => state.analytics);
  const hasHydrated = useCookieConsentStore((state) => state._hasHydrated);

  useEffect(() => {
    if (!hasHydrated) {
      return;
    }

    setAnalyticsConsent(analytics === true);
  }, [analytics, hasHydrated]);

  useEffect(() => {
    if (analytics !== true) {
      return;
    }

    trackPageView(
      buildPagePath(location.pathname, location.search, location.hash),
    );
  }, [location.pathname, location.search, location.hash, analytics]);

  return null;
}
