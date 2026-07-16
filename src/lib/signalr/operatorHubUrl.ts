/**
 * Hub URLs live on the API host root (e.g. /hubs/notifications), not under /api.
 */
export function operatorHubUrl(apiBaseUrl: string, hubPath: string): string {
  const root = apiBaseUrl.replace(/\/api\/?$/, "")
  const path = hubPath.startsWith("/") ? hubPath : `/${hubPath}`
  return `${root}${path}`
}
