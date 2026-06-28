declare global {
  interface Window {
    dataLayer?: unknown[];
    gtag?: Gtag;
  }
}

type Gtag = (...args: unknown[]) => void;

export const GA_MEASUREMENT_ID = import.meta.env.VITE_GA_MEASUREMENT_ID;

let consentGranted = false;
let initialized = false;

export function isAnalyticsEnabled(): boolean {
  return Boolean(GA_MEASUREMENT_ID);
}

export function setAnalyticsConsent(granted: boolean): void {
  consentGranted = granted;

  if (granted) {
    initGoogleAnalytics();
    return;
  }

  disableGoogleAnalytics();
}

export function initGoogleAnalytics(): void {
  if (
    !isAnalyticsEnabled() ||
    !consentGranted ||
    initialized ||
    typeof window === "undefined"
  ) {
    return;
  }

  initialized = true;

  if (GA_MEASUREMENT_ID) {
    delete (window as Record<string, unknown>)[`ga-disable-${GA_MEASUREMENT_ID}`];
  }

  window.dataLayer = window.dataLayer ?? [];
  window.gtag = function gtag(...args: unknown[]) {
    window.dataLayer!.push(args);
  };

  window.gtag("js", new Date());
  window.gtag("config", GA_MEASUREMENT_ID);

  const script = document.createElement("script");
  script.async = true;
  script.src = `https://www.googletagmanager.com/gtag/js?id=${GA_MEASUREMENT_ID}`;
  document.head.appendChild(script);
}

export function disableGoogleAnalytics(): void {
  if (GA_MEASUREMENT_ID) {
    (window as Record<string, unknown>)[`ga-disable-${GA_MEASUREMENT_ID}`] =
      true;
  }

  initialized = false;
}

export function trackPageView(path: string): void {
  if (!isAnalyticsEnabled() || !consentGranted || !window.gtag) {
    return;
  }

  window.gtag("config", GA_MEASUREMENT_ID, {
    page_path: path,
  });
}
