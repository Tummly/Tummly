export function canPurgeTrialData(): boolean {
  if (import.meta.env.DEV) {
    return true;
  }

  return import.meta.env.VITE_APP_ENV === "qa";
}
