import { describe, expect, it, vi } from "vitest"

import { createLiveStaffRedeemAdapters } from "@/lib/operatorOffers/staffRedeemAdapters"

describe("createLiveStaffRedeemAdapters", () => {
  it("maps successful check preview from the API", async () => {
    const checkStaffRedeemCode = vi.fn(async () => ({
      success: true as const,
      preview: {
        issueId: "42",
        offerTitle: "10% off next visit",
        guestName: "Maya",
        validAt: "Soho",
        expires: "19 Aug 2026, 12:00pm",
        usage: "Single-use",
        staffInstruction: "Apply 10% off.",
      },
    }))
    const adapters = createLiveStaffRedeemAdapters({
      checkStaffRedeemCode,
      markStaffRedeemed: vi.fn(),
    })

    const result = await adapters.checkCode(7, "TUM-ABCDEF")

    expect(checkStaffRedeemCode).toHaveBeenCalledWith({
      locationId: 7,
      code: "TUM-ABCDEF",
    })
    expect(result).toEqual({
      ok: true,
      preview: {
        issueId: "42",
        offerTitle: "10% off next visit",
        guestName: "Maya",
        validAt: "Soho",
        expires: "19 Aug 2026, 12:00pm",
        usage: "Single-use",
        staffInstruction: "Apply 10% off.",
      },
    })
  })

  it.each([
    "invalid",
    "expired",
    "already_used",
    "voided",
    "wrong_location",
  ] as const)("maps check failure reason %s", async (reason) => {
    const adapters = createLiveStaffRedeemAdapters({
      checkStaffRedeemCode: vi.fn(async () => ({
        success: false as const,
        reason,
      })),
      markStaffRedeemed: vi.fn(),
    })

    const result = await adapters.checkCode(1, "TUM-XXXXXX")
    expect(result).toEqual({ ok: false, reason })
  })

  it("treats unknown check failure reason as invalid", async () => {
    const adapters = createLiveStaffRedeemAdapters({
      checkStaffRedeemCode: vi.fn(async () =>
        ({
          success: false,
          reason: "mystery",
        }) as unknown as { success: false; reason: "invalid" }
      ),
      markStaffRedeemed: vi.fn(),
    })

    const result = await adapters.checkCode(1, "X")
    expect(result).toEqual({ ok: false, reason: "invalid" })
  })

  it("maps redeem success and failure", async () => {
    const markStaffRedeemed = vi
      .fn()
      .mockResolvedValueOnce({ success: true })
      .mockResolvedValueOnce({ success: false, reason: "already_used" })
    const adapters = createLiveStaffRedeemAdapters({
      checkStaffRedeemCode: vi.fn(),
      markStaffRedeemed,
    })

    await expect(
      adapters.redeem(3, "TUM-ABCDEF", "9")
    ).resolves.toEqual({ ok: true })
    expect(markStaffRedeemed).toHaveBeenCalledWith({
      locationId: 3,
      code: "TUM-ABCDEF",
      issueId: "9",
    })

    await expect(
      adapters.redeem(3, "TUM-ABCDEF", "9")
    ).resolves.toEqual({ ok: false })
  })
})
