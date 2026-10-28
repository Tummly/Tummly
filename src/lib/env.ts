export function canPurgeTrialData(): boolean {
  if (import.meta.env.DEV) {
    return true;
  }

  return import.meta.env.VITE_APP_ENV === "qa";
}

/**
 * Build-time environment name for the staff banner when the API environment
 * is not loaded. Null means no banner (production build without `qa`).
 */
export function clientEnvironmentName(): string | null {
  if (import.meta.env.DEV) {
    return "Development";
  }

  return import.meta.env.VITE_APP_ENV === "qa" ? "QA" : null;
}

/** Owner Admin-column matrix edits — off by default; set VITE_TEAM_PERMISSIONS_MATRIX_EDIT=true to enable. */
export function isTeamPermissionsMatrixEditEnabled(): boolean {
  return import.meta.env.VITE_TEAM_PERMISSIONS_MATRIX_EDIT === "true";
}
