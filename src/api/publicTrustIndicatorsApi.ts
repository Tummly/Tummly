import axiosInstance from "@/api/axiosInstance"

/**
 * Anonymous public read of the published trust indicators. `skipAuthRedirect`
 * stops a failure from clearing the session or redirecting to login; this
 * request has no toast path.
 */
export async function fetchPublicTrustIndicators(): Promise<unknown> {
  const response = await axiosInstance.get<unknown>("/public/trust-indicators", {
    skipAuthRedirect: true,
  })
  return response.data
}
