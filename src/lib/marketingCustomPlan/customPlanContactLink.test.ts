import { describe, expect, it } from "vitest"

import {
  buildCustomPlanContactHref,
  buildCustomPlanContactMessage,
  locationCountOptionFor,
} from "@/lib/marketingCustomPlan/customPlanContactLink"
import { EMPTY_PACK_COUNTS } from "@/lib/marketingCustomPlan/customPlanEstimate"

describe("locationCountOptionFor", () => {
  it.each([
    [1, "1"],
    [3, "2-3"],
    [5, "4-5"],
    [10, "6-10"],
    [20, "11-20"],
    [30, "21-30"],
    [31, "30+"],
    [650, "30+"],
  ])("maps %i Locations to %s", (locations, option) => {
    expect(locationCountOptionFor(locations)).toBe(option)
  })

  it("leaves the field empty with no Locations", () => {
    expect(locationCountOptionFor(0)).toBe("")
  })
})

describe("buildCustomPlanContactMessage", () => {
  it("lists the Location count, billing and each chosen pack", () => {
    const message = buildCustomPlanContactMessage({
      locations: 650,
      cadence: "annual",
      packs: { ...EMPTY_PACK_COUNTS, team_user: 2, email_20k: 1 },
    })

    expect(message).toBe(
      [
        "I would like a Tummly plan for 650 Locations.",
        "",
        "Billing: Annual",
        "Additional packs:",
        "- Additional team users × 2",
        "- +20,000 Email/month × 1",
      ].join("\n"),
    )
  })

  it("says none when no packs are chosen", () => {
    const message = buildCustomPlanContactMessage({
      locations: 1,
      cadence: "monthly",
      packs: EMPTY_PACK_COUNTS,
    })

    expect(message).toContain("I would like a Tummly plan for 1 Location.")
    expect(message).toContain("Billing: Monthly")
    expect(message).toContain("Additional packs: none")
  })
})

describe("buildCustomPlanContactHref", () => {
  it("opens the contact form on Plans and pricing with the plan filled in", () => {
    const href = buildCustomPlanContactHref({
      locations: 650,
      cadence: "monthly",
      packs: { ...EMPTY_PACK_COUNTS, offer: 3 },
    })
    const url = new URL(href, "https://tummly.test")

    expect(url.pathname).toBe("/help-center/contact")
    expect(url.searchParams.get("topic")).toBe("plans-and-pricing")
    expect(url.searchParams.get("locations")).toBe("30+")
    expect(url.searchParams.get("message")).toContain(
      "- Additional active Offers × 3",
    )
  })
})
