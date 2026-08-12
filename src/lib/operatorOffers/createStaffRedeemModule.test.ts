import { describe, expect, it, vi } from "vitest"

import {
  createStubStaffRedeemAdapters,
  type StaffRedeemAdapters,
} from "@/lib/operatorOffers/staffRedeemAdapters"
import { createStaffRedeemModule } from "@/lib/operatorOffers/createStaffRedeemModule"
import { STAFF_REDEEM_COPY } from "@/lib/operatorOffers/staffRedeemPresentation"

function createModule(adapters?: Partial<StaffRedeemAdapters>) {
  return createStaffRedeemModule(createStubStaffRedeemAdapters(adapters))
}

describe("createStaffRedeemModule", () => {
  it("caches getSnapshot for useSyncExternalStore identity", () => {
    const module = createModule()
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("opens on enter-code for a location and clears prior state", async () => {
    const module = createModule()
    module.open(42)
    module.setCode("USED")
    await module.checkOffer()
    expect(module.getSnapshot().checkError).toBe(
      STAFF_REDEEM_COPY.errors.already_used
    )

    module.close()
    module.open(7)

    const snapshot = module.getSnapshot()
    expect(snapshot.open).toBe(true)
    expect(snapshot.locationId).toBe(7)
    expect(snapshot.step).toBe("enter-code")
    expect(snapshot.code).toBe("")
    expect(snapshot.checkError).toBeNull()
    expect(snapshot.confirmPreview).toBeNull()
  })

  it("keeps code and shows inline error when check fails", async () => {
    const checkCode = vi.fn(async () => ({
      ok: false as const,
      reason: "expired" as const,
    }))
    const module = createModule({ checkCode })
    module.open(1)
    module.setCode("EXPIRED")

    await module.checkOffer()

    expect(checkCode).toHaveBeenCalledWith(1, "EXPIRED")
    const snapshot = module.getSnapshot()
    expect(snapshot.step).toBe("enter-code")
    expect(snapshot.code).toBe("EXPIRED")
    expect(snapshot.checkError).toBe(STAFF_REDEEM_COPY.errors.expired)
    expect(snapshot.checkBusy).toBe(false)
  })

  it("maps distinct check failure reasons to copy", async () => {
    const reasons = [
      "invalid",
      "expired",
      "already_used",
      "voided",
      "wrong_location",
    ] as const

    for (const reason of reasons) {
      const module = createModule({
        checkCode: async () => ({ ok: false, reason }),
      })
      module.open(1)
      module.setCode("x")
      await module.checkOffer()
      expect(module.getSnapshot().checkError).toBe(
        STAFF_REDEEM_COPY.errors[reason]
      )
    }
  })

  it("moves to confirm with preview when check succeeds", async () => {
    const module = createModule()
    module.open(3)
    module.setCode("OK-MAYA")

    await module.checkOffer()

    const snapshot = module.getSnapshot()
    expect(snapshot.step).toBe("confirm")
    expect(snapshot.checkError).toBeNull()
    expect(snapshot.confirmPreview).toMatchObject({
      offerTitle: "10% off next visit",
      guestName: "Maya",
      validAt: "Camden",
      expires: "Today, 11:59pm",
      usage: "Single-use",
    })
    expect(snapshot.confirmPreview?.staffInstruction.length).toBeGreaterThan(0)
  })

  it("rejects empty code without calling the adapter", async () => {
    const checkCode = vi.fn()
    const module = createModule({ checkCode })
    module.open(1)
    module.setCode("   ")

    await module.checkOffer()

    expect(checkCode).not.toHaveBeenCalled()
    expect(module.getSnapshot().checkError).toBe(
      STAFF_REDEEM_COPY.errors.empty_code
    )
  })

  it("applyScannedCode fills code and runs check only", async () => {
    const redeem = vi.fn(async () => ({ ok: true as const }))
    const module = createModule({ redeem })
    module.open(9)

    await module.applyScannedCode("OK-SCAN")

    const snapshot = module.getSnapshot()
    expect(snapshot.code).toBe("OK-SCAN")
    expect(snapshot.step).toBe("confirm")
    expect(redeem).not.toHaveBeenCalled()
  })

  it("cancelConfirm returns to enter-code and keeps code", async () => {
    const module = createModule()
    module.open(1)
    module.setCode("OK-KEEP")
    await module.checkOffer()
    expect(module.getSnapshot().step).toBe("confirm")

    module.cancelConfirm()

    const snapshot = module.getSnapshot()
    expect(snapshot.step).toBe("enter-code")
    expect(snapshot.code).toBe("OK-KEEP")
    expect(snapshot.confirmPreview).toBeNull()
    expect(snapshot.open).toBe(true)
  })

  it("close closes the dialogue", () => {
    const module = createModule()
    module.open(1)
    module.setCode("abc")
    module.close()
    expect(module.getSnapshot().open).toBe(false)
  })

  it("markAsRedeemed closes on success", async () => {
    const redeem = vi.fn(async () => ({ ok: true as const }))
    const module = createModule({ redeem })
    module.open(5)
    module.setCode("OK-1")
    await module.checkOffer()

    const result = await module.markAsRedeemed()

    expect(result).toBe("redeemed")
    expect(redeem).toHaveBeenCalledWith(5, "OK-1", expect.any(String))
    expect(module.getSnapshot().open).toBe(false)
  })

  it("markAsRedeemed stays on confirm when redeem fails", async () => {
    const module = createModule({
      redeem: async () => ({ ok: false }),
    })
    module.open(5)
    module.setCode("OK-1")
    await module.checkOffer()

    const result = await module.markAsRedeemed()

    expect(result).toBe("failed")
    const snapshot = module.getSnapshot()
    expect(snapshot.open).toBe(true)
    expect(snapshot.step).toBe("confirm")
    expect(snapshot.checkError).toBe(STAFF_REDEEM_COPY.errors.redeem_failed)
  })

  it("markAsRedeemed is noop when not on confirm", async () => {
    const redeem = vi.fn()
    const module = createModule({ redeem })
    module.open(1)

    expect(await module.markAsRedeemed()).toBe("noop")
    expect(redeem).not.toHaveBeenCalled()
  })

  it("ignores stale check results after close", async () => {
    let resolveCheck!: (value: {
      ok: true
      preview: {
        issueId: string
        offerTitle: string
        guestName: string
        validAt: string
        expires: string
        usage: string
        staffInstruction: string
      }
    }) => void

    const module = createModule({
      checkCode: () =>
        new Promise((resolve) => {
          resolveCheck = resolve
        }),
    })
    module.open(1)
    module.setCode("OK-LATE")
    const pending = module.checkOffer()
    expect(module.getSnapshot().checkBusy).toBe(true)

    module.close()
    resolveCheck({
      ok: true,
      preview: {
        issueId: "late",
        offerTitle: "Late",
        guestName: "X",
        validAt: "Y",
        expires: "Z",
        usage: "Single-use",
        staffInstruction: "Do not apply",
      },
    })
    await pending

    expect(module.getSnapshot().open).toBe(false)
    expect(module.getSnapshot().step).toBe("enter-code")
    expect(module.getSnapshot().confirmPreview).toBeNull()
  })

  it("clears check error when the code changes", async () => {
    const module = createModule()
    module.open(1)
    module.setCode("USED")
    await module.checkOffer()
    expect(module.getSnapshot().checkError).not.toBeNull()

    module.setCode("USED-2")
    expect(module.getSnapshot().checkError).toBeNull()
  })
})
