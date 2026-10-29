/** Query key on `/scan/:token` and `GET /api/scan/:token` for an operator Preview visit. */
export const GUEST_FORM_PREVIEW_PARAM = "preview"

const GUEST_FORM_PREVIEW_VALUE = "true"

/** Home Preview opens the live guest link with the flag so the visit is not counted as a scan. */
export function buildGuestFormPreviewUrl(scanUrl: string): string {
  const separator = scanUrl.includes("?") ? "&" : "?"
  return `${scanUrl}${separator}${GUEST_FORM_PREVIEW_PARAM}=${GUEST_FORM_PREVIEW_VALUE}`
}

export function isGuestFormPreviewParam(value: string | null): boolean {
  return value === GUEST_FORM_PREVIEW_VALUE
}
