import { describe, expect, it } from "vitest"
import {
  REPORTS_BREADCRUMB_COPY,
  REPORTS_HUB_PAGE_COPY,
  REPORTS_PAGE_STACK_CLASS,
  REPORTS_PAGE_TITLE_CLASS,
  REPORTS_SECTION_CLASS,
  REPORTS_STANDARD_ACTIONS_COPY,
  REPORTS_TABLE_FRAME_CLASS,
  resolveReportsStatusBadgeVariant,
} from "@/lib/operatorReports/reportsPresentation"

describe("reportsPresentation", () => {
  it("exports shared hub copy and standard header action labels", () => {
    expect(REPORTS_HUB_PAGE_COPY.title).toBe("Reports")
    expect(REPORTS_HUB_PAGE_COPY.emptyTitle).toBe("No report data yet.")
    expect(REPORTS_BREADCRUMB_COPY.reports).toBe("Reports")
    expect(REPORTS_STANDARD_ACTIONS_COPY.generateBrief).toBe("Generate brief")
    expect(REPORTS_STANDARD_ACTIONS_COPY.export).toBe("Export")
  })

  it("exports chrome class tokens aligned with Operator page shells", () => {
    expect(REPORTS_PAGE_STACK_CLASS).toContain("flex")
    expect(REPORTS_PAGE_STACK_CLASS).toContain("gap-5")
    expect(REPORTS_PAGE_TITLE_CLASS).toContain("text-2xl")
    expect(REPORTS_PAGE_TITLE_CLASS).toContain("sm:text-[32px]")
    expect(REPORTS_SECTION_CLASS).toContain("rounded-op-lg")
    expect(REPORTS_TABLE_FRAME_CLASS).toContain("border")
  })

  it("maps report status labels to Badge variants", () => {
    expect(resolveReportsStatusBadgeVariant("Active")).toBe("ready")
    expect(resolveReportsStatusBadgeVariant("Paused")).toBe("soft")
    expect(resolveReportsStatusBadgeVariant("Redeemed")).toBe("ready")
    expect(resolveReportsStatusBadgeVariant("Follow-up needed")).toBe("neutral")
    expect(resolveReportsStatusBadgeVariant("Invalid")).toBe("negative")
    expect(resolveReportsStatusBadgeVariant("Something else")).toBe("soft")
  })
})
