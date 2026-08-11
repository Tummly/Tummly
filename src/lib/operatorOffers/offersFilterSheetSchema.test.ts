import { describe, expect, it } from "vitest"

import {
  OFFERS_ATTACH_SOURCE_LABELS,
  OFFERS_STATUS_LABELS,
  offersFilterSheetSchema,
} from "@/lib/operatorOffers/offersFilterSheetSchema"
import { buildOffersListQueryParams } from "@/lib/operatorOffers/offersListQueryParams"
import {
  chipCount,
  emptySelection,
  projectChips,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"

describe("offersFilterSheetSchema", () => {
  it("exposes Status and Offer type (attach source) only", () => {
    const schema = offersFilterSheetSchema()
    expect(schema.fields.map((field) => field.id)).toEqual([
      "status",
      "attachSource",
    ])
    expect(schema.fields.some((field) => field.id === "date")).toBe(false)
    expect(schema.fields.some((field) => field.id === "location")).toBe(false)
  })

  it("projects chips and maps query params for Status + attachSource", () => {
    const schema = offersFilterSheetSchema()
    const filters: OperatorFilterSelection = {
      ...emptySelection(schema),
      status: { kind: "multi-select", ids: ["draft", "active"] },
      attachSource: { kind: "multi-select", ids: ["campaign", "manual"] },
    }

    const chips = projectChips(schema, filters)
    expect(chips.map((chip) => chip.label)).toEqual([
      OFFERS_STATUS_LABELS.draft,
      OFFERS_STATUS_LABELS.active,
      OFFERS_ATTACH_SOURCE_LABELS.campaign,
      OFFERS_ATTACH_SOURCE_LABELS.manual,
    ])
    expect(chipCount(schema, filters)).toBe(4)

    const params = buildOffersListQueryParams({
      locationId: 42,
      view: "all",
      q: "  brunch  ",
      sort: "title-az",
      page: 2,
      pageSize: 25,
      filters,
      now: new Date("2026-08-11T12:00:00.000Z"),
    })

    expect(params).toMatchObject({
      locationId: 42,
      view: "all",
      q: "brunch",
      sort: "title-az",
      page: 2,
      pageSize: 25,
      status: ["draft", "active"],
      attachSource: ["campaign", "manual"],
    })
    expect(typeof params.utcOffsetMinutes).toBe("number")
  })
})
