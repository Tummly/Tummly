import { describe, expect, it, vi } from "vitest"

import { createGuestActivityTabModule } from "@/lib/operatorGuestProfile/createGuestActivityTabModule"
import { guestActivityFilterSheetSchema } from "@/lib/operatorGuestProfile/guestActivityFilterSheetSchema"
import { emptySelection, type OperatorFilterSelection } from "@/lib/operatorFilterSheet"
import type { GuestActivityListQueryParams } from "@/lib/operatorGuestProfile/guestActivityListQueryParams"
import type { GuestActivityListResponse } from "@/types/dashboard"

const ACTIVITY_SCHEMA = guestActivityFilterSheetSchema()

function filters(
  overrides: Record<string, OperatorFilterSelection[string]>
): OperatorFilterSelection {
  return { ...emptySelection(ACTIVITY_SCHEMA), ...overrides }
}

function listResponse(
  overrides?: Partial<GuestActivityListResponse>
): GuestActivityListResponse {
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
  kind: string
  occurredAt?: string
  locationName?: string
  tagName?: string | null
  authorDisplayName?: string | null
  sentiment?: string | null
  changedFields?: string[] | null
  feedbackId?: number | null
}) {
  return {
    id: overrides.id,
    kind: overrides.kind,
    occurredAt: overrides.occurredAt ?? "2026-07-15T18:42:00Z",
    feedbackId: overrides.feedbackId ?? null,
    locationName: overrides.locationName ?? "Soho",
    tagName: overrides.tagName ?? null,
    guestTagId: null,
    authorDisplayName: overrides.authorDisplayName ?? null,
    sentiment: overrides.sentiment ?? null,
    changedFields: overrides.changedFields ?? null,
  }
}

describe("createGuestActivityTabModule", () => {
  it("shows virgin-empty chrome when unfiltered total is 0", async () => {
    const getGuestActivity = vi.fn(async () => listResponse())
    const module = createGuestActivityTabModule({ getGuestActivity })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    const snapshot = module.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(snapshot.viewModel?.timelineEmptyState).toBe("virgin-empty")
    expect(snapshot.viewModel?.toolbarEnabled).toBe(false)
    expect(getGuestActivity).toHaveBeenCalledWith(
      expect.objectContaining({
        guestId: 12,
        locationId: 3,
        sort: "recent-activity",
        page: 1,
        pageSize: 25,
      } satisfies Partial<GuestActivityListQueryParams>)
    )
  })

  it("enables toolbar and maps PRD row copy for in-map kinds", async () => {
    const getGuestActivity = vi.fn(async () =>
      listResponse({
        totalCount: 8,
        items: [
          item({ id: 1, kind: "guest-joined" }),
          item({
            id: 2,
            kind: "feedback",
            sentiment: "negative",
          }),
          item({ id: 3, kind: "feedback", sentiment: null }),
          item({
            id: 4,
            kind: "classification-succeeded",
            sentiment: "positive",
          }),
          item({ id: 5, kind: "classification-failed" }),
          item({
            id: 6,
            kind: "note-added",
            authorDisplayName: "Alex Morgan",
          }),
          item({
            id: 7,
            kind: "tag-applied",
            tagName: "VIP",
          }),
          item({
            id: 8,
            kind: "profile-edited",
            changedFields: ["name", "email"],
          }),
        ],
      })
    )
    const module = createGuestActivityTabModule({ getGuestActivity })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    const vm = module.getSnapshot().viewModel
    expect(vm?.toolbarEnabled).toBe(true)
    expect(vm?.timelineEmptyState).toBeNull()
    expect(vm?.timelineRows).toEqual([
      {
        id: 1,
        headline: "Guest joined",
        body: "Guest joined through the Guest QR form.",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
      {
        id: 2,
        headline: "Feedback received",
        body: "Negative feedback received through the Guest QR form.",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
      {
        id: 3,
        headline: "Feedback received",
        body: "Feedback received through the Guest QR form.",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
      {
        id: 4,
        headline: "Classification succeeded",
        body: "Feedback classified as Positive.",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
      {
        id: 5,
        headline: "Classification failed",
        body: "Feedback classification failed.",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
      {
        id: 6,
        headline: "Note added",
        body: "Note added by Alex Morgan.",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
      {
        id: 7,
        headline: "Tag applied",
        body: "Tag “VIP” applied.",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
      {
        id: 8,
        headline: "Profile updated",
        body: "Profile details updated (Name, Email).",
        metaDisplay: "Soho · 15 July 2026, 7:42 PM",
      },
    ])
  })

  it("shows filtered-empty when type/date filters yield zero rows", async () => {
    const getGuestActivity = vi
      .fn()
      .mockResolvedValueOnce(
        listResponse({
          totalCount: 1,
          items: [item({ id: 1, kind: "guest-joined" })],
        })
      )
      .mockResolvedValueOnce(listResponse({ totalCount: 0, items: [] }))

    const module = createGuestActivityTabModule({ getGuestActivity })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    module.applyFilters(
      filters({
        activityType: { kind: "multi-select", ids: ["note"] },
      })
    )
    await vi.waitFor(() => {
      expect(module.getSnapshot().viewModel?.timelineEmptyState).toBe(
        "filtered-empty"
      )
    })

    expect(module.getSnapshot().viewModel?.toolbarEnabled).toBe(true)
    expect(getGuestActivity).toHaveBeenLastCalledWith(
      expect.objectContaining({
        type: ["note"],
      })
    )
  })

  it("sends datePreset with utcOffsetMinutes when date filter applied", async () => {
    const getGuestActivity = vi.fn(async () =>
      listResponse({
        totalCount: 1,
        items: [item({ id: 1, kind: "feedback" })],
      })
    )
    const module = createGuestActivityTabModule({
      getGuestActivity,
      getNow: () => new Date("2026-07-23T12:00:00+01:00"),
    })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    module.applyFilters(
      filters({
        date: { kind: "date", value: { kind: "preset", preset: "last-7" } },
      })
    )
    await vi.waitFor(() => {
      expect(module.getSnapshot().filterChipCount).toBe(1)
    })

    expect(getGuestActivity).toHaveBeenLastCalledWith(
      expect.objectContaining({
        datePreset: "last-7",
        utcOffsetMinutes: expect.any(Number),
      })
    )
  })

  it("clearFilters keeps sort and reloads", async () => {
    const getGuestActivity = vi.fn(
      async (): Promise<GuestActivityListResponse> =>
        listResponse({
          totalCount: 1,
          items: [item({ id: 1, kind: "guest-joined" })],
        })
    )
    const module = createGuestActivityTabModule({ getGuestActivity })

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
        activityType: { kind: "multi-select", ids: ["tag"] },
      })
    )
    await vi.waitFor(() => {
      expect(module.getSnapshot().filterChipCount).toBe(1)
    })

    module.clearFilters()
    await vi.waitFor(() => {
      expect(module.getSnapshot().filterChipCount).toBe(0)
    })

    expect(module.getSnapshot().sortId).toBe("oldest-first")
    const lastCall = getGuestActivity.mock.calls.at(-1)?.[0]
    expect(lastCall).toMatchObject({ sort: "oldest-first" })
    expect(lastCall?.type).toBeUndefined()
  })

  it("does not fetch while inactive, then loads on activate", async () => {
    const getGuestActivity = vi.fn(async () => listResponse())
    const module = createGuestActivityTabModule({ getGuestActivity })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: false,
    })
    expect(getGuestActivity).not.toHaveBeenCalled()

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })
    expect(getGuestActivity).toHaveBeenCalledTimes(1)
  })

  it("returns a stable getSnapshot reference until publish", async () => {
    const getGuestActivity = vi.fn(async () => listResponse())
    const module = createGuestActivityTabModule({ getGuestActivity })

    const first = module.getSnapshot()
    const second = module.getSnapshot()
    expect(second).toBe(first)

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })
    expect(module.getSnapshot()).not.toBe(first)
  })

  it("maps tag-removed headline and body", async () => {
    const getGuestActivity = vi.fn(async () =>
      listResponse({
        totalCount: 1,
        items: [
          item({
            id: 9,
            kind: "tag-removed",
            tagName: "Allergy",
          }),
        ],
      })
    )
    const module = createGuestActivityTabModule({ getGuestActivity })

    await module.syncWorkspace({
      guestId: 12,
      selectedLocationId: 3,
      active: true,
    })

    expect(module.getSnapshot().viewModel?.timelineRows[0]).toEqual({
      id: 9,
      headline: "Tag removed",
      body: "Tag “Allergy” removed.",
      metaDisplay: "Soho · 15 July 2026, 7:42 PM",
    })
  })
})
