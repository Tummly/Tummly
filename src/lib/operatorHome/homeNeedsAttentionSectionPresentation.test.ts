import { describe, expect, it } from "vitest"

import { buildHomeNeedsAttention } from "./buildHomeNeedsAttention"
import {
  NEEDS_ATTENTION_LOAD_ERROR,
  NEEDS_ATTENTION_VIEW_ALL_LABEL,
  resolveHomeNeedsAttentionSectionBody,
  WARNING_ROW_CLASS,
} from "./homeNeedsAttentionSectionPresentation"

describe("resolveHomeNeedsAttentionSectionBody", () => {
  it("returns loading for idle and loading so the rest of Home can still render", () => {
    expect(
      resolveHomeNeedsAttentionSectionBody({
        loadStatus: "idle",
        projection: null,
        errorMessage: null,
        expanded: false,
      })
    ).toEqual({ mode: "loading" })
    expect(
      resolveHomeNeedsAttentionSectionBody({
        loadStatus: "loading",
        projection: null,
        errorMessage: null,
        expanded: false,
      })
    ).toEqual({ mode: "loading" })
  })

  it("returns honest error copy plus Retry payload on load failure", () => {
    expect(
      resolveHomeNeedsAttentionSectionBody({
        loadStatus: "error",
        projection: null,
        errorMessage: null,
        expanded: false,
      })
    ).toEqual({
      mode: "error",
      message: NEEDS_ATTENTION_LOAD_ERROR,
    })
  })

  it("keeps the accordion empty copy when the projection has no rows", () => {
    const projection = buildHomeNeedsAttention({
      locationName: "Manchester",
      feedback: { count: 0, newestSubmittedAt: null },
    })
    expect(
      resolveHomeNeedsAttentionSectionBody({
        loadStatus: "loaded",
        projection,
        errorMessage: null,
        expanded: false,
      })
    ).toEqual({ mode: "empty" })
  })

  it("shows the first five rows until View all expands in place", () => {
    const projection = buildHomeNeedsAttention({
      locationName: "Manchester",
      nowMs: Date.parse("2026-08-21T12:00:00.000Z"),
      campaigns: [1, 2, 3, 4, 5, 6].map((id) => ({
        id,
        name: `Campaign ${id}`,
        status: "failed" as const,
        updatedAt: `2026-08-21T11:0${id}:00.000Z`,
        rowVersion: `rv-${id}`,
      })),
    })
    expect(projection.showViewAll).toBe(true)
    expect(projection.visibleRows).toHaveLength(5)

    const collapsed = resolveHomeNeedsAttentionSectionBody({
      loadStatus: "loaded",
      projection,
      errorMessage: null,
      expanded: false,
    })
    expect(collapsed).toEqual({
      mode: "rows",
      rows: projection.visibleRows,
      showViewAll: true,
    })

    const expanded = resolveHomeNeedsAttentionSectionBody({
      loadStatus: "loaded",
      projection,
      errorMessage: null,
      expanded: true,
    })
    expect(expanded).toEqual({
      mode: "rows",
      rows: projection.allRows,
      showViewAll: false,
    })
    expect(expanded.mode === "rows" ? expanded.rows : []).toHaveLength(6)
  })
})

describe("Home Needs attention row chrome", () => {
  it("reuses Offers warning-row wash and padding", () => {
    expect(WARNING_ROW_CLASS).toContain("bg-op-background-secondary")
    expect(WARNING_ROW_CLASS).toContain("rounded-[4px]")
    expect(WARNING_ROW_CLASS).toContain("pl-[30px]")
    expect(NEEDS_ATTENTION_VIEW_ALL_LABEL).toBe("View all")
  })
})
