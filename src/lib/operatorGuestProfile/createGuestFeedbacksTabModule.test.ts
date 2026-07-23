import { describe, expect, it, vi } from "vitest"

import { createGuestFeedbacksTabModule } from "@/lib/operatorGuestProfile/createGuestFeedbacksTabModule"
import { guestFeedbacksFilterSheetSchema } from "@/lib/operatorGuestProfile/guestFeedbacksFilterSheetSchema"
import { emptySelection, type OperatorFilterSelection } from "@/lib/operatorFilterSheet"
import type { GuestFeedbacksListResponse } from "@/types/dashboard"
import type { GuestFeedbacksListQueryParams } from "@/lib/operatorGuestProfile/guestFeedbacksListQueryParams"

const FEEDBACKS_SCHEMA = guestFeedbacksFilterSheetSchema()

function filters(
  overrides: Record<string, OperatorFilterSelection[string]>
): OperatorFilterSelection {
  return { ...emptySelection(FEEDBACKS_SCHEMA), ...overrides }
}

function listResponse(
  overrides?: Partial<GuestFeedbacksListResponse>
): GuestFeedbacksListResponse {
  return {
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 25,
    ...overrides,
  }
}

function item(overrides: {
  id: number
  comment: string
  createdAt?: string
  classificationStatus?: "Pending" | "Succeeded" | "Failed"
  sentiment?: "positive" | "neutral" | "negative" | null
  detectedTags?: string[] | null
}) {
  return {
    id: overrides.id,
    createdAt: overrides.createdAt ?? "2026-07-15T18:42:00Z",
    comment: overrides.comment,
    locationName: "Soho",
    classificationStatus: overrides.classificationStatus ?? "Succeeded",
    sentiment:
      overrides.sentiment === undefined
        ? "negative"
        : overrides.sentiment,
    detectedTags:
      overrides.detectedTags === undefined
        ? ["Service"]
        : overrides.detectedTags,
  }
}

describe("createGuestFeedbacksTabModule", () => {
  it("shows virgin-empty chrome when unfiltered total is 0", async () => {
    const getGuestFeedbacks = vi.fn(async () => listResponse())
    const module = createGuestFeedbacksTabModule({
      getGuestFeedbacks,
      debounceMs: 0,
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    const snapshot = module.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.tableEmptyState).toBe("virgin-empty")
    expect(snapshot.viewModel?.toolbarEnabled).toBe(false)
    expect(getGuestFeedbacks).toHaveBeenCalledWith(
      expect.objectContaining({
        guestId: 12,
        locationId: 3,
        sort: "recent-activity",
        page: 1,
        pageSize: 25,
      } satisfies Partial<GuestFeedbacksListQueryParams>)
    )
  })

  it("enables toolbar and maps Location as Guest QR form · name", async () => {
    const getGuestFeedbacks = vi.fn(async () =>
      listResponse({
        totalCount: 1,
        items: [
          item({
            id: 99,
            comment: "Too cold",
            classificationStatus: "Succeeded",
            sentiment: "negative",
            detectedTags: ["FoodQuality"],
          }),
        ],
      })
    )
    const module = createGuestFeedbacksTabModule({
      getGuestFeedbacks,
      debounceMs: 0,
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    const row = module.getSnapshot().viewModel?.tableRows[0]
    expect(module.getSnapshot().viewModel?.toolbarEnabled).toBe(true)
    expect(module.getSnapshot().viewModel?.tableEmptyState).toBeNull()
    expect(row?.locationDisplay).toBe("Guest QR form · Soho")
    expect(row?.recoveryDisplay).toBe("—")
    expect(row?.classificationDisplay).toBe("negative")
    expect(row?.issueTagLabels).toEqual(["Food quality"])
  })

  it("shows filtered-empty when search/filters yield zero rows", async () => {
    const getGuestFeedbacks = vi
      .fn()
      .mockResolvedValueOnce(
        listResponse({
          totalCount: 1,
          items: [item({ id: 1, comment: "Hello" })],
        })
      )
      .mockResolvedValueOnce(listResponse({ totalCount: 0, items: [] }))

    const module = createGuestFeedbacksTabModule({
      getGuestFeedbacks,
      debounceMs: 0,
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    module.setSearchQuery("nope")
    await vi.waitFor(() => {
      expect(module.getSnapshot().viewModel?.tableEmptyState).toBe(
        "filtered-empty"
      )
    })

    expect(module.getSnapshot().viewModel?.toolbarEnabled).toBe(true)
    expect(getGuestFeedbacks).toHaveBeenLastCalledWith(
      expect.objectContaining({ q: "nope" })
    )
  })

  it("clearSearchAndFilters keeps sort and reloads", async () => {
    const getGuestFeedbacks = vi.fn(async () =>
      listResponse({
        totalCount: 1,
        items: [item({ id: 1, comment: "Hello" })],
      })
    )
    const module = createGuestFeedbacksTabModule({
      getGuestFeedbacks,
      debounceMs: 0,
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    module.setSortId("oldest-first")
    await vi.waitFor(() => {
      expect(module.getSnapshot().sortId).toBe("oldest-first")
    })

    module.applyFilters(
      filters({
        sentiment: { kind: "multi-select", ids: ["negative"] },
      })
    )
    await vi.waitFor(() => {
      expect(module.getSnapshot().filterChipCount).toBe(1)
    })

    module.clearSearchAndFilters()
    await vi.waitFor(() => {
      expect(module.getSnapshot().filterChipCount).toBe(0)
    })

    expect(module.getSnapshot().sortId).toBe("oldest-first")
    expect(module.getSnapshot().searchQuery).toBe("")
  })

  it("does not fetch while inactive, then loads on activate", async () => {
    const getGuestFeedbacks = vi.fn(async () => listResponse())
    const module = createGuestFeedbacksTabModule({
      getGuestFeedbacks,
      debounceMs: 0,
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: false,
    })
    expect(getGuestFeedbacks).not.toHaveBeenCalled()

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })
    expect(getGuestFeedbacks).toHaveBeenCalledTimes(1)
  })

  it("pages with goToNextPage / goToPreviousPage", async () => {
    const getGuestFeedbacks = vi.fn(
      async (params: GuestFeedbacksListQueryParams) =>
        listResponse({
          totalCount: 30,
          page: params.page,
          pageSize: 25,
          items: [
            item({
              id: params.page,
              comment: `Page ${params.page}`,
            }),
          ],
        })
    )
    const module = createGuestFeedbacksTabModule({
      getGuestFeedbacks,
      debounceMs: 0,
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    module.goToNextPage()
    await vi.waitFor(() => {
      expect(module.getSnapshot().viewModel?.currentPage).toBe(2)
    })
    expect(getGuestFeedbacks).toHaveBeenLastCalledWith(
      expect.objectContaining({ page: 2 })
    )

    module.goToPreviousPage()
    await vi.waitFor(() => {
      expect(module.getSnapshot().viewModel?.currentPage).toBe(1)
    })
  })

  it("maps Succeeded with empty issue tags as calm empty (not em dash)", async () => {
    const getGuestFeedbacks = vi.fn(async () =>
      listResponse({
        totalCount: 1,
        items: [
          item({
            id: 7,
            comment: "Fine",
            classificationStatus: "Succeeded",
            sentiment: "positive",
            detectedTags: [],
          }),
        ],
      })
    )
    const module = createGuestFeedbacksTabModule({
      getGuestFeedbacks,
      debounceMs: 0,
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    const row = module.getSnapshot().viewModel?.tableRows[0]
    expect(row?.issueTagLabels).toEqual([])
    expect(row?.classificationDisplay).toBe("positive")
  })
})
