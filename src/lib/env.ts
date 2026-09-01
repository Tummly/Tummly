export function canPurgeTrialData(): boolean {
  if (import.meta.env.DEV) {
    return true;
  }

  return import.meta.env.VITE_APP_ENV === "qa";
}

/** Owner Admin-column matrix edits — off by default; set VITE_TEAM_PERMISSIONS_MATRIX_EDIT=true to enable. */
export function isTeamPermissionsMatrixEditEnabled(): boolean {
  return import.meta.env.VITE_TEAM_PERMISSIONS_MATRIX_EDIT === "true";
}
