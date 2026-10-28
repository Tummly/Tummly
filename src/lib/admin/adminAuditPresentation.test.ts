import { describe, expect, it } from "vitest"

import type { AdminAuditEvent } from "@/types/adminOps"

import {
  auditActorLabel,
  auditPageRangeText,
  auditResultLabel,
  auditTargetLabel,
  buildAdminAuditQuery,
  EMPTY_AUDIT_FILTERS,
  formatAuditDetail,
} from "./adminAuditPresentation"

const PAGING = { skip: 0, take: 50 }

describe("buildAdminAuditQuery", () => {
  it("sends only paging when every filter is blank", () => {
    expect(buildAdminAuditQuery(EMPTY_AUDIT_FILTERS, PAGING)).toEqual({
      ok: true,
      params: { skip: 0, take: 50 },
    })
  })

  it("sends trimmed action, target id and restaurant id", () => {
    const result = buildAdminAuditQuery(
      { ...EMPTY_AUDIT_FILTERS, action: " payment.refund ", targetId: " ord_1 ", restaurantId: "42" },
      PAGING
    )

    expect(result).toEqual({
      ok: true,
      params: { skip: 0, take: 50, action: "payment.refund", targetId: "ord_1", restaurantId: 42 },
    })
  })

  it("converts London calendar days to UTC instants", () => {
    const result = buildAdminAuditQuery(
      { ...EMPTY_AUDIT_FILTERS, from: "2026-10-01", to: "2026-10-01" },
      PAGING
    )

    expect(result).toEqual({
      ok: true,
      params: {
        skip: 0,
        take: 50,
        from: "2026-09-30T23:00:00.000Z",
        to: "2026-10-01T22:59:59.999Z",
      },
    })
  })

  it("rejects a restaurant id that is not a positive whole number", () => {
    expect(buildAdminAuditQuery({ ...EMPTY_AUDIT_FILTERS, restaurantId: "4.2" }, PAGING)).toEqual({
      ok: false,
      error: "Enter a restaurant ID as a whole number.",
    })
    expect(buildAdminAuditQuery({ ...EMPTY_AUDIT_FILTERS, restaurantId: "0" }, PAGING).ok).toBe(false)
  })

  it("rejects a from date after the to date", () => {
    expect(
      buildAdminAuditQuery({ ...EMPTY_AUDIT_FILTERS, from: "2026-10-05", to: "2026-10-01" }, PAGING)
    ).toEqual({ ok: false, error: "The from date must be on or before the to date." })
  })

  it("rejects an impossible date", () => {
    expect(
      buildAdminAuditQuery({ ...EMPTY_AUDIT_FILTERS, from: "2026-02-31" }, PAGING)
    ).toEqual({ ok: false, error: "Enter a valid from date." })
  })
})

describe("formatAuditDetail", () => {
  it("pretty-prints JSON detail", () => {
    expect(formatAuditDetail('{"qty":10,"reason":"fix"}')).toEqual({
      text: '{\n  "qty": 10,\n  "reason": "fix"\n}',
      isJson: true,
    })
  })

  it("shows non-JSON detail as written", () => {
    expect(formatAuditDetail("plain note")).toEqual({ text: "plain note", isJson: false })
  })

  it("explains when there is no detail", () => {
    expect(formatAuditDetail(null)).toEqual({ text: "No detail recorded.", isJson: false })
  })
})

describe("audit row labels", () => {
  const base: Pick<AdminAuditEvent, "actorAdminUserId" | "actorIdentity" | "targetType" | "targetId" | "succeeded"> = {
    actorAdminUserId: 9,
    actorIdentity: "admin@tummly.com",
    targetType: "payment_order",
    targetId: "ord_1",
    succeeded: false,
  }

  it("labels the result", () => {
    expect(auditResultLabel(base)).toBe("Failed")
    expect(auditResultLabel({ succeeded: true })).toBe("Succeeded")
  })

  it("labels a background job actor as System", () => {
    expect(auditActorLabel({ actorAdminUserId: null, actorIdentity: "" })).toBe("System")
    expect(auditActorLabel({ actorAdminUserId: 9, actorIdentity: "" })).toBe("Admin #9")
    expect(auditActorLabel(base)).toBe("admin@tummly.com")
  })

  it("labels the target", () => {
    expect(auditTargetLabel(base)).toBe("payment_order ord_1")
  })
})

describe("auditPageRangeText", () => {
  it("shows the row range, and never a range past the end", () => {
    expect(auditPageRangeText(0, 50, 120)).toBe("Showing 1–50 of 120")
    expect(auditPageRangeText(50, 0, 50)).toBe("No events on this page (50 in total)")
    expect(auditPageRangeText(0, 0, 0)).toBe("No events")
  })
})
