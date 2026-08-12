import type {
  StaffRedeemCheckApiResponse,
  StaffRedeemMarkApiResponse,
} from "@/types/operatorCampaigns"
import { STAFF_REDEEM_COPY } from "@/lib/operatorOffers/staffRedeemPresentation"

export type StaffRedeemCheckFailureReason =
  | "invalid"
  | "expired"
  | "already_used"
  | "voided"
  | "wrong_location"

const CHECK_FAILURE_REASONS = new Set<StaffRedeemCheckFailureReason>([
  "invalid",
  "expired",
  "already_used",
  "voided",
  "wrong_location",
])

function parseCheckFailureReason(
  reason: unknown
): StaffRedeemCheckFailureReason {
  if (
    typeof reason === "string"
    && CHECK_FAILURE_REASONS.has(reason as StaffRedeemCheckFailureReason)
  ) {
    return reason as StaffRedeemCheckFailureReason
  }
  return "invalid"
}

export type StaffRedeemConfirmPreview = {
  /** Offer issue id — passed to Mark as redeemed. */
  issueId: string
  offerTitle: string
  guestName: string
  validAt: string
  expires: string
  usage: string
  staffInstruction: string
}

export type StaffRedeemCheckSuccess = {
  ok: true
  preview: StaffRedeemConfirmPreview
}

export type StaffRedeemCheckFailure = {
  ok: false
  reason: StaffRedeemCheckFailureReason
}

export type StaffRedeemCheckResult =
  | StaffRedeemCheckSuccess
  | StaffRedeemCheckFailure

export type StaffRedeemRedeemResult =
  | { ok: true }
  | { ok: false }

export type StaffRedeemAdapters = {
  checkCode: (
    locationId: number,
    code: string
  ) => Promise<StaffRedeemCheckResult>
  redeem: (
    locationId: number,
    code: string,
    issueId: string
  ) => Promise<StaffRedeemRedeemResult>
}

export function staffRedeemErrorMessage(
  reason: StaffRedeemCheckFailureReason | "empty_code" | "redeem_failed"
): string {
  return STAFF_REDEEM_COPY.errors[reason]
}

export type StaffRedeemApiClient = {
  checkStaffRedeemCode: (body: {
    locationId: number
    code: string
  }) => Promise<StaffRedeemCheckApiResponse>
  markStaffRedeemed: (body: {
    locationId: number
    code: string
    issueId: string
  }) => Promise<StaffRedeemMarkApiResponse>
}

/** Production path — live Check + Mark as redeemed against Offer Claim codes. */
export function createLiveStaffRedeemAdapters(
  api: StaffRedeemApiClient
): StaffRedeemAdapters {
  return {
    async checkCode(locationId, code) {
      const response = await api.checkStaffRedeemCode({ locationId, code })
      if (response.success) {
        return { ok: true, preview: response.preview }
      }
      return {
        ok: false,
        reason: parseCheckFailureReason(response.reason),
      }
    },
    async redeem(locationId, code, issueId) {
      const response = await api.markStaffRedeemed({
        locationId,
        code,
        issueId,
      })
      return response.success ? { ok: true } : { ok: false }
    },
  }
}

/**
 * Stub for unit tests only — not the production provider path.
 * Codes:
 * - `OK-*` → success preview
 * - `EXPIRED` / `USED` / `VOID` / `WRONGLOC` / anything else → matching failure
 */
export function createStubStaffRedeemAdapters(
  overrides?: Partial<StaffRedeemAdapters>
): StaffRedeemAdapters {
  const base: StaffRedeemAdapters = {
    async checkCode(_locationId, code) {
      const trimmed = code.trim()
      if (trimmed.toUpperCase().startsWith("OK-")) {
        return {
          ok: true,
          preview: {
            issueId: `issue-${trimmed}`,
            offerTitle: "10% off next visit",
            guestName: "Maya",
            validAt: "Camden",
            expires: "Today, 11:59pm",
            usage: "Single-use",
            staffInstruction:
              "Apply 10% off the order before payment. Tap redeem once the discount has been applied.",
          },
        }
      }
      if (trimmed.toUpperCase() === "EXPIRED") {
        return { ok: false, reason: "expired" }
      }
      if (trimmed.toUpperCase() === "USED") {
        return { ok: false, reason: "already_used" }
      }
      if (trimmed.toUpperCase() === "VOID") {
        return { ok: false, reason: "voided" }
      }
      if (trimmed.toUpperCase() === "WRONGLOC") {
        return { ok: false, reason: "wrong_location" }
      }
      return { ok: false, reason: "invalid" }
    },
    async redeem(_locationId, _code, _issueId) {
      return { ok: true }
    },
  }

  return { ...base, ...overrides }
}
