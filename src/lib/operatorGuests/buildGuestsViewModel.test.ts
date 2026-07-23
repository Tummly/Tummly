import { describe, expect, it } from "vitest"

import { buildOperatorGuestsViewModel, resolveGuestsTableEmptyStateKind } from "@/lib/operatorGuests/buildGuestsViewModel"
import {
  OPERATOR_GUEST_FIXTURES,
  OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
  assertGuestFixtureCoverageMinimums,
} from "@/lib/operatorGuests/guestFixtures"
import {
  countGuestsBySmartGroup,
  guestMatchesSmartGroup,
} from "@/lib/operatorGuests/smartGroupPredicates"

const NOW_MS = OPERATOR_GUEST_FIXTURES_REFERENCE_MS

describe("operator guest fixtures", () => {
  it("ships 40 guests meeting coverage minimums", () => {
    expect(OPERATOR_GUEST_FIXTURES).toHaveLength(40)

    assertGuestFixtureCoverageMinimums(OPERATOR_GUEST_FIXTURES, NOW_MS, {
      newGuests: 6,
      needsRecovery: 5,
      positiveFeedback: 8,
      offerNotRedeemed: 5,
      recentRedeemers: 4,
      dormantGuests: 4,
      newThisMonth: 15,
      marketingEligible: 20,
    })
  })
})

describe("smart group predicates", () => {
  it("allows multi-membership across smart groups", () => {
    const overlapGuest = OPERATOR_GUEST_FIXTURES.find(
      (guest) => guest.id === "guest-002"
    )
    expect(overlapGuest).toBeDefined()

    expect(
      guestMatchesSmartGroup(overlapGuest!, "new-guests", NOW_MS)
    ).toBe(true)
    expect(
      guestMatchesSmartGroup(overlapGuest!, "positive-feedback", NOW_MS)
    ).toBe(true)
    expect(
      guestMatchesSmartGroup(overlapGuest!, "offer-not-redeemed", NOW_MS)
    ).toBe(true)
  })

  it("does not treat null last interaction as dormant", () => {
    const noInteractionGuest = OPERATOR_GUEST_FIXTURES.find(
      (guest) => guest.lastInteractionAt == null
    )
    expect(noInteractionGuest).toBeDefined()
    expect(
      guestMatchesSmartGroup(noInteractionGuest!, "dormant-guests", NOW_MS)
    ).toBe(false)
  })

  it("counts needs recovery from the needsRecovery flag", () => {
    expect(
      countGuestsBySmartGroup(
        OPERATOR_GUEST_FIXTURES,
        "needs-recovery",
        NOW_MS
      )
    ).toBe(5)
  })
})

describe("buildOperatorGuestsViewModel", () => {
  it("derives overview KPIs from the full fixture set", () => {
    const viewModel = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      nowMs: NOW_MS,
    })

    expect(viewModel.overviewKpis).toEqual([
      expect.objectContaining({ id: "total-guests", value: 40 }),
      expect.objectContaining({ id: "new-this-month", value: 27 }),
      expect.objectContaining({ id: "marketing-eligible", value: 35 }),
      expect.objectContaining({ id: "needs-recovery", value: 5 }),
    ])
  })

  it("filters table rows when the active smart group changes", () => {
    const allGuests = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      nowMs: NOW_MS,
    })
    const needsRecovery = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "needs-recovery",
      nowMs: NOW_MS,
    })

    expect(allGuests.tableRows).toHaveLength(25)
    expect(allGuests.totalFilteredCount).toBe(40)
    expect(needsRecovery.totalFilteredCount).toBe(5)
    expect(needsRecovery.tableRows.every((row) =>
      ["Isla Fraser", "Jack Morrison", "Kate Sullivan", "Leo Ahmed", "Mia Robertson"].includes(
        row.name
      )
    )).toBe(true)
  })

  it("shows smart group tab counts derived from fixtures", () => {
    const viewModel = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      nowMs: NOW_MS,
    })

    expect(viewModel.smartGroupTabs).toEqual([
      expect.objectContaining({ id: "all-guests", count: 40 }),
      expect.objectContaining({ id: "new-guests", count: 12 }),
      expect.objectContaining({ id: "needs-recovery", count: 5 }),
      expect.objectContaining({ id: "positive-feedback", count: 22 }),
      expect.objectContaining({ id: "offer-not-redeemed", count: 9 }),
      expect.objectContaining({ id: "recent-redeemers", count: 4 }),
      expect.objectContaining({ id: "dormant-guests", count: 4 }),
    ])
  })

  it("defaults to recent activity sort and the first page of 25 rows", () => {
    const viewModel = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      nowMs: NOW_MS,
    })

    expect(viewModel.sortLabel).toBe("Recent activity")
    expect(viewModel.tableRows).toHaveLength(25)
    expect(viewModel.pageRangeLabel).toBe("Showing 1–25 of 40 guests")
    expect(viewModel.tableRows[0]?.name).toBe("Amelia Hughes")
  })

  it("filters table rows by guest name, email, or mobile", () => {
    const byName = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      searchQuery: "amelia",
      nowMs: NOW_MS,
    })
    const byEmail = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      searchQuery: "isla.fraser@example.com",
      nowMs: NOW_MS,
    })
    const byMobile = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      searchQuery: "900111",
      nowMs: NOW_MS,
    })

    expect(byName.totalFilteredCount).toBe(1)
    expect(byName.tableRows[0]?.name).toBe("Amelia Hughes")
    expect(byEmail.totalFilteredCount).toBe(1)
    expect(byEmail.tableRows[0]?.name).toBe("Isla Fraser")
    expect(byMobile.totalFilteredCount).toBe(1)
    expect(byMobile.tableRows[0]?.name).toBe("Kate Sullivan")
  })

  it("combines search with the active smart group", () => {
    const viewModel = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "needs-recovery",
      searchQuery: "isla",
      nowMs: NOW_MS,
    })

    expect(viewModel.totalFilteredCount).toBe(1)
    expect(viewModel.tableRows[0]?.name).toBe("Isla Fraser")
  })

  it("sorts guests by all seven sort options", () => {
    const nameAz = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      sortId: "guest-name-az",
      nowMs: NOW_MS,
    })
    const nameZa = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      sortId: "guest-name-za",
      nowMs: NOW_MS,
    })
    const newest = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      sortId: "newest-guests",
      nowMs: NOW_MS,
    })
    const oldest = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      sortId: "oldest-guests",
      nowMs: NOW_MS,
    })
    const mostFeedback = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      sortId: "most-feedback-submissions",
      nowMs: NOW_MS,
    })
    const mostRedemption = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      sortId: "most-recent-redemption",
      nowMs: NOW_MS,
    })

    expect(nameAz.tableRows[0]?.name).toBe("Aaron Brooks")
    expect(nameZa.tableRows[0]?.name).toBe("Zara Mitchell")
    expect(newest.tableRows[0]?.name).toBe("Amelia Hughes")
    expect(oldest.tableRows[0]?.name).toBe("Nora Blake")
    expect(mostFeedback.tableRows[0]?.feedbackSubmissionCount).toBeGreaterThan(
      mostFeedback.tableRows[1]?.feedbackSubmissionCount ?? 0
    )
    expect(mostRedemption.sortLabel).toBe("Most recent redemption")
  })

  it("paginates at 25 rows per page with correct range labels", () => {
    const pageOne = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      page: 1,
      nowMs: NOW_MS,
    })
    const pageTwo = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      page: 2,
      nowMs: NOW_MS,
    })
    const empty = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      searchQuery: "no-such-guest",
      nowMs: NOW_MS,
    })

    expect(pageOne.currentPage).toBe(1)
    expect(pageOne.pageSize).toBe(25)
    expect(pageOne.tableRows).toHaveLength(25)
    expect(pageTwo.currentPage).toBe(2)
    expect(pageTwo.tableRows).toHaveLength(15)
    expect(pageTwo.pageRangeLabel).toBe("Showing 26–40 of 40 guests")
    expect(empty.pageRangeLabel).toBe("Showing 0 of 0 guests")
    expect(empty.tableRows).toHaveLength(0)
    expect(empty.tableEmptyState).toBe("no-guests-found")
  })

  it("returns no-guests-yet when the fixture list is empty", () => {
    const viewModel = buildOperatorGuestsViewModel({
      guests: [],
      activeSmartGroupId: "all-guests",
      nowMs: NOW_MS,
    })

    expect(viewModel.tableEmptyState).toBe("no-guests-yet")
    expect(viewModel.tableRows).toHaveLength(0)
    expect(viewModel.overviewKpis).toEqual([
      expect.objectContaining({ id: "total-guests", value: 0 }),
      expect.objectContaining({ id: "new-this-month", value: 0 }),
      expect.objectContaining({ id: "marketing-eligible", value: 0 }),
      expect.objectContaining({ id: "needs-recovery", value: 0 }),
    ])
  })

  it("returns null table empty state when rows are visible", () => {
    const viewModel = buildOperatorGuestsViewModel({
      guests: OPERATOR_GUEST_FIXTURES,
      activeSmartGroupId: "all-guests",
      nowMs: NOW_MS,
    })

    expect(viewModel.tableEmptyState).toBeNull()
  })
})

describe("resolveGuestsTableEmptyStateKind", () => {
  it("returns no-guests-yet when the active smart group has zero guests", () => {
    expect(resolveGuestsTableEmptyStateKind(0, 0)).toBe("no-guests-yet")
  })

  it("returns no-guests-found when the smart group has guests but search/filters match nothing", () => {
    expect(resolveGuestsTableEmptyStateKind(40, 0)).toBe("no-guests-found")
  })

  it("returns null when filtered rows exist", () => {
    expect(resolveGuestsTableEmptyStateKind(40, 5)).toBeNull()
  })
})
