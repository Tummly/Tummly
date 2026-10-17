import axiosInstance from "@/api/axiosInstance"
import {
  parseVerifyOtpResponse,
  type VerifyOtpPayload,
} from "@/pages/utils/authHelpers"

export type DemoGuestEntry = {
  restaurantName: string
  locationName: string
  scanToken: string
  scanPath: string
}

/** POST /api/auth/demo-session — anonymous JWT for the seeded product-demo owner. */
export async function createDemoSession(): Promise<VerifyOtpPayload> {
  const response = await axiosInstance.post("/auth/demo-session", null, {
    skipAuthRedirect: true,
  })
  const session = parseVerifyOtpResponse(response.data)
  if (session == null) {
    throw new Error("Demo session response was incomplete.")
  }
  return session
}

/** GET /api/auth/demo-guest-entry — stable Mavero Guest Form scan path. */
export async function fetchDemoGuestEntry(): Promise<DemoGuestEntry> {
  const response = await axiosInstance.get("/auth/demo-guest-entry", {
    skipAuthRedirect: true,
  })
  const payload = response.data as {
    success?: boolean
    data?: DemoGuestEntry
    message?: string
  }
  if (!payload?.data?.scanPath || !payload.data.scanToken) {
    throw new Error(payload?.message ?? "Demo guest entry unavailable.")
  }
  return payload.data
}
