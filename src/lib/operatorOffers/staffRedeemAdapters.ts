import { STAFF_REDEEM_COPY } from "@/lib/operatorOffers/staffRedeemPresentation"

export type StaffRedeemCheckFailureReason =
  | "invalid"
  | "expired"
  | "already_used"
  | "voided"
  | "wrong_location"

export type StaffRedeemConfirmPreview = {
  /** Stable id for the redeemable issue — passed to redeem when APIs land. */
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

/**
 * Honest stub for tests and UI until redeem APIs ship.
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
