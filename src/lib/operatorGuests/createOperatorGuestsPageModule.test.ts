import { describe, expect, it } from "vitest"

import { createOperatorGuestsPageModule } from "@/lib/operatorGuests/createOperatorGuestsPageModule"
import { OPERATOR_GUEST_FIXTURES_REFERENCE_MS } from "@/lib/operatorGuests/guestFixtures"

describe("createOperatorGuestsPageModule", () => {
  it("updates the view model when the active smart group changes", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    const updates: number[] = []

    module.subscribe(() => {
      updates.push(module.getSnapshot().viewModel.totalFilteredCount)
    })

    expect(module.getSnapshot().viewModel.activeSmartGroupId).toBe("all-guests")
    expect(module.getSnapshot().viewModel.totalFilteredCount).toBe(40)

    module.setActiveSmartGroupId("needs-recovery")

    expect(module.getSnapshot().viewModel.activeSmartGroupId).toBe(
      "needs-recovery"
    )
    expect(module.getSnapshot().viewModel.totalFilteredCount).toBe(5)
    expect(updates).toEqual([5])
  })

  it("does not emit when setting the same smart group", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    let updateCount = 0

    module.subscribe(() => {
      updateCount += 1
    })

    module.setActiveSmartGroupId("all-guests")

    expect(updateCount).toBe(0)
  })

  it("returns a stable snapshot reference until state changes", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })

    const first = module.getSnapshot()
    const second = module.getSnapshot()

    expect(first).toBe(second)

    module.setActiveSmartGroupId("needs-recovery")

    expect(module.getSnapshot()).not.toBe(first)
  })

  it("starts with no selection and no bulk bar label", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })

    expect(module.getSnapshot().selectedGuestIds).toEqual([])
    expect(module.getSnapshot().selectedCount).toBe(0)
    expect(module.getSnapshot().bulkSelectionLabel).toBeNull()
    expect(module.getSnapshot().isAllVisibleSelected).toBe(false)
    expect(module.getSnapshot().isSomeVisibleSelected).toBe(false)
  })

  it("toggles row selection and exposes bulk bar copy", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    const [firstRow, secondRow] = module.getSnapshot().viewModel.tableRows

    module.toggleGuestSelection(firstRow!.id)

    expect(module.getSnapshot().selectedGuestIds).toEqual([firstRow!.id])
    expect(module.getSnapshot().selectedCount).toBe(1)
    expect(module.getSnapshot().bulkSelectionLabel).toBe("1 guest selected")
    expect(module.getSnapshot().isGuestSelected(firstRow!.id)).toBe(true)
    expect(module.getSnapshot().isSomeVisibleSelected).toBe(true)

    module.toggleGuestSelection(secondRow!.id)

    expect(module.getSnapshot().selectedCount).toBe(2)
    expect(module.getSnapshot().bulkSelectionLabel).toBe("2 guests selected")

    module.toggleGuestSelection(firstRow!.id)

    expect(module.getSnapshot().selectedCount).toBe(1)
    expect(module.getSnapshot().isGuestSelected(firstRow!.id)).toBe(false)
  })

  it("selects and deselects all visible rows via the header control", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    const visibleIds = module
      .getSnapshot()
      .viewModel.tableRows.map((row) => row.id)

    module.toggleSelectAllVisibleRows()

    expect(module.getSnapshot().selectedGuestIds).toEqual([...visibleIds].sort())
    expect(module.getSnapshot().selectedCount).toBe(25)
    expect(module.getSnapshot().isAllVisibleSelected).toBe(true)
    expect(module.getSnapshot().isSomeVisibleSelected).toBe(false)
    expect(module.getSnapshot().bulkSelectionLabel).toBe("25 guests selected")

    module.toggleSelectAllVisibleRows()

    expect(module.getSnapshot().selectedGuestIds).toEqual([])
    expect(module.getSnapshot().selectedCount).toBe(0)
    expect(module.getSnapshot().bulkSelectionLabel).toBeNull()
  })

  it("clears selection", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    const firstRow = module.getSnapshot().viewModel.tableRows[0]!

    module.toggleGuestSelection(firstRow.id)
    module.toggleGuestSelection(
      module.getSnapshot().viewModel.tableRows[1]!.id
    )

    module.clearSelection()

    expect(module.getSnapshot().selectedGuestIds).toEqual([])
    expect(module.getSnapshot().selectedCount).toBe(0)
    expect(module.getSnapshot().bulkSelectionLabel).toBeNull()
  })

  it("does not emit when clearing an already empty selection", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    let updateCount = 0

    module.subscribe(() => {
      updateCount += 1
    })

    module.clearSelection()

    expect(updateCount).toBe(0)
  })

  it("recomputes visible selection flags when the smart group changes", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    const recoveryRow = module
      .getSnapshot()
      .viewModel.tableRows.find((row) => row.name === "Isla Fraser")
    const nonRecoveryRow = module
      .getSnapshot()
      .viewModel.tableRows.find((row) => row.name === "Amelia Hughes")

    expect(recoveryRow).toBeDefined()
    expect(nonRecoveryRow).toBeDefined()

    module.toggleGuestSelection(recoveryRow!.id)
    module.toggleGuestSelection(nonRecoveryRow!.id)
    module.setActiveSmartGroupId("needs-recovery")

    expect(module.getSnapshot().selectedCount).toBe(2)
    expect(module.getSnapshot().viewModel.tableRows).toHaveLength(5)
    expect(module.getSnapshot().isAllVisibleSelected).toBe(false)
    expect(module.getSnapshot().isSomeVisibleSelected).toBe(true)
    expect(module.getSnapshot().isGuestSelected(recoveryRow!.id)).toBe(true)
    expect(module.getSnapshot().isGuestSelected(nonRecoveryRow!.id)).toBe(true)
  })

  it("updates search, sort, and pagination state", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })

    module.setSearchQuery("isla")
    expect(module.getSnapshot().searchQuery).toBe("isla")
    expect(module.getSnapshot().viewModel.totalFilteredCount).toBe(1)
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)

    module.setSortId("guest-name-za")
    expect(module.getSnapshot().sortId).toBe("guest-name-za")
    expect(module.getSnapshot().viewModel.sortLabel).toBe("Guest name Z–A")
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)

    module.goToNextPage()
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)

    module.setSearchQuery("")
    module.goToNextPage()
    expect(module.getSnapshot().viewModel.currentPage).toBe(2)
    expect(module.getSnapshot().viewModel.pageRangeLabel).toBe(
      "Showing 26–40 of 40 guests"
    )

    module.goToPreviousPage()
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)
  })

  it("resets page to 1 when search, sort, or smart group changes", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })

    module.goToNextPage()
    expect(module.getSnapshot().viewModel.currentPage).toBe(2)

    module.setSearchQuery("amelia")
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)

    module.goToNextPage()
    module.setSortId("guest-name-az")
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)

    module.goToNextPage()
    module.setActiveSmartGroupId("needs-recovery")
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)
  })

  it("clamps page when filtered results shrink", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })

    module.goToNextPage()
    expect(module.getSnapshot().viewModel.currentPage).toBe(2)

    module.setSearchQuery("isla")
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)
    expect(module.getSnapshot().viewModel.totalFilteredCount).toBe(1)
  })

  it("does not emit when setting the same search query or sort", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    let updateCount = 0

    module.subscribe(() => {
      updateCount += 1
    })

    module.setSearchQuery("")
    module.setSortId("recent-activity")

    expect(updateCount).toBe(0)
  })

  it("exposes no-guests-yet when created with an empty fixture list", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
      guests: [],
    })

    expect(module.getSnapshot().viewModel.tableEmptyState).toBe("no-guests-yet")
    expect(module.getSnapshot().viewModel.tableRows).toHaveLength(0)
  })

  it("clears search and resets smart group to all guests", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })

    module.setActiveSmartGroupId("needs-recovery")
    module.setSearchQuery("isla")
    expect(module.getSnapshot().viewModel.tableEmptyState).toBeNull()

    module.setSearchQuery("no-such-guest")
    expect(module.getSnapshot().viewModel.tableEmptyState).toBe("no-guests-found")
    expect(module.getSnapshot().searchQuery).toBe("no-such-guest")
    expect(module.getSnapshot().viewModel.activeSmartGroupId).toBe(
      "needs-recovery"
    )

    module.clearSearchAndFilters()

    expect(module.getSnapshot().searchQuery).toBe("")
    expect(module.getSnapshot().viewModel.activeSmartGroupId).toBe("all-guests")
    expect(module.getSnapshot().viewModel.tableEmptyState).toBeNull()
    expect(module.getSnapshot().viewModel.currentPage).toBe(1)
  })

  it("does not emit when clearSearchAndFilters is already reset", () => {
    const module = createOperatorGuestsPageModule({
      nowMs: OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
    })
    let updateCount = 0

    module.subscribe(() => {
      updateCount += 1
    })

    module.clearSearchAndFilters()

    expect(updateCount).toBe(0)
  })
})
