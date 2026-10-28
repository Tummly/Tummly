import type { AdminEnvironment } from "@/types/adminOps"

/**
 * Text for the non-production banner, or null when no banner should show.
 * The API decides production (`isProduction`). When the API environment is
 * not available (staff without Admin access), the build-time name is used.
 */
export function environmentBannerLabel(
  apiEnvironment: AdminEnvironment | null,
  buildEnvironmentName: string | null
): string | null {
  if (apiEnvironment) {
    return apiEnvironment.isProduction ? null : apiEnvironment.environmentName
  }
  return buildEnvironmentName
}
