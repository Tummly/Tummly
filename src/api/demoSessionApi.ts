import axiosInstance from "@/api/axiosInstance"
import {
  parseVerifyOtpResponse,
  type VerifyOtpPayload,
} from "@/pages/utils/authHelpers"

/** POST /api/auth/demo-session — anonymous JWT for the seeded product-demo owner. */
export async function createDemoSession(): Promise<VerifyOtpPayload> {
  const response = await axiosInstance.post("/auth/demo-session", null, {
    skipAuthRedirect: true,
  })
  const parsed = parseVerifyOtpResponse(response.data)
  if (!parsed) {
    throw new Error("Demo session response was invalid.")
  }
  return parsed
}
