import { describe, expect, it, vi } from "vitest"

import { createGuestProfileFilteredListKernel } from "@/lib/operatorGuestProfile/createGuestProfileFilteredListKernel"

type Filters = { type: string[] }
type ViewModel = {
  totalCount: number
  pageSize: number
  toolbarEnabled: boolean
}

function createKernel(
  loadImpl: () => Promise<ViewModel> = async () => ({
    totalCount: 30,
    pageSize: 25,
    toolbarEnabled: true,
  })
) {
  const load = vi.fn(loadImpl)
  return {
    load,
    kernel: createGuestProfileFilteredListKernel<
      ViewModel,
      "recent" | "oldest",
      Filters,
      { draft: boolean }
    >({
      defaultSortId: "recent",
      emptyFilters: () => ({ type: [] }),
      load,
    }),
  }
}

const activeWorkspace = { guestId: 12, selectedLocationId: 3, active: true }

describe("createGuestProfileFilteredListKernel", () => {
  it("does not fetch while inactive, then fetches on activation", async () => {
    const { kernel, load } = createKernel()
    await kernel.syncWorkspace({ ...activeWorkspace, active: false })
    expect(load).not.toHaveBeenCalled()

    await kernel.syncWorkspace(activeWorkspace)
    expect(load).toHaveBeenCalledTimes(1)
  })

  it("resets without fetching for a null guest or location", async () => {
    const { kernel, load } = createKernel()
    await kernel.syncWorkspace(activeWorkspace)
    await kernel.syncWorkspace({ ...activeWorkspace, guestId: null })

    expect(kernel.getCoreSnapshot()).toMatchObject({
      loadStatus: "idle",
      viewModel: null,
    })
    expect(load).toHaveBeenCalledTimes(1)
  })

  it("resets and reloads when the guest or location changes", async () => {
    const { kernel, load } = createKernel()
    await kernel.syncWorkspace(activeWorkspace)
    await kernel.syncWorkspace({ ...activeWorkspace, selectedLocationId: 4 })

    expect(load).toHaveBeenLastCalledWith(
      expect.objectContaining({ locationId: 4, page: 1 })
    )
  })

  it("does not refetch a successfully loaded pair", async () => {
    const { kernel, load } = createKernel()
    await kernel.syncWorkspace(activeWorkspace)
    await kernel.syncWorkspace(activeWorkspace)

    expect(load).toHaveBeenCalledTimes(1)
    expect(kernel.getCoreSnapshot().loadStatus).toBe("loaded")
  })

  it("refetches after a first-load error for the same pair", async () => {
    const load = vi
      .fn()
      .mockRejectedValueOnce(new Error("offline"))
      .mockResolvedValueOnce({
        totalCount: 1,
        pageSize: 25,
        toolbarEnabled: true,
      })
    const { kernel } = createKernel(load)
    await kernel.syncWorkspace(activeWorkspace)
    expect(kernel.getCoreSnapshot().loadStatus).toBe("error")

    await kernel.syncWorkspace(activeWorkspace)
    expect(load).toHaveBeenCalledTimes(2)
    expect(kernel.getCoreSnapshot().loadStatus).toBe("loaded")
  })

  it("ignores a stale generation response", async () => {
    let resolveFirst!: (value: ViewModel) => void
    const first = new Promise<ViewModel>(resolve => {
      resolveFirst = resolve
    })
    const load = vi
      .fn()
      .mockReturnValueOnce(first)
      .mockResolvedValueOnce({
        totalCount: 1,
        pageSize: 25,
        toolbarEnabled: true,
      })
    const { kernel } = createKernel(load)

    const firstSync = kernel.syncWorkspace(activeWorkspace)
    await kernel.syncWorkspace({ ...activeWorkspace, selectedLocationId: 4 })
    resolveFirst({ totalCount: 99, pageSize: 25, toolbarEnabled: true })
    await firstSync

    expect(kernel.getCoreSnapshot().viewModel?.totalCount).toBe(1)
  })

  it("resets to page one on sort and honors the toolbar lock", async () => {
    const { kernel, load } = createKernel()
    await kernel.syncWorkspace(activeWorkspace)
    kernel.goToNextPage()
    await vi.waitFor(() =>
      expect(kernel.getCoreSnapshot().page).toBe(2)
    )
    kernel.setSortId("oldest")
    await vi.waitFor(() =>
      expect(kernel.getCoreSnapshot()).toMatchObject({ sortId: "oldest", page: 1 })
    )

    const locked = createKernel(async () => ({
      totalCount: 0,
      pageSize: 25,
      toolbarEnabled: false,
    }))
    await locked.kernel.syncWorkspace(activeWorkspace)
    locked.kernel.setSortId("oldest")
    expect(locked.kernel.getCoreSnapshot().sortId).toBe("recent")
    expect(load).toHaveBeenCalled()
  })

  it("keeps paging within its bounds", async () => {
    const { kernel, load } = createKernel()
    await kernel.syncWorkspace(activeWorkspace)
    kernel.goToPreviousPage()
    kernel.goToNextPage()
    await vi.waitFor(() => expect(kernel.getCoreSnapshot().page).toBe(2))
    kernel.goToNextPage()
    expect(load).toHaveBeenCalledTimes(2)
  })

  it("applies and clears filters at page one, closing the session", async () => {
    const { kernel, load } = createKernel()
    await kernel.syncWorkspace(activeWorkspace)
    kernel.openFiltersSession({ draft: true })
    kernel.applyFilters({ type: ["note"] })
    await vi.waitFor(() =>
      expect(kernel.getCoreSnapshot()).toMatchObject({
        page: 1,
        appliedFilters: { type: ["note"] },
        filtersSession: null,
      })
    )

    kernel.clearFilters()
    await vi.waitFor(() =>
      expect(kernel.getCoreSnapshot().appliedFilters).toEqual({ type: [] })
    )
    expect(load).toHaveBeenLastCalledWith(
      expect.objectContaining({ filters: { type: [] }, page: 1 })
    )
  })

  it("replaceFilters reloads without closing an open session", async () => {
    const { kernel } = createKernel()
    await kernel.syncWorkspace(activeWorkspace)
    kernel.openFiltersSession({ draft: true })
    kernel.replaceFilters({ type: ["tag"] })
    await vi.waitFor(() =>
      expect(kernel.getCoreSnapshot()).toMatchObject({
        appliedFilters: { type: ["tag"] },
        filtersSession: { draft: true },
        page: 1,
      })
    )
  })

  it("blocks opening a filter session when the toolbar is locked", async () => {
    const { kernel } = createKernel(async () => ({
      totalCount: 0,
      pageSize: 25,
      toolbarEnabled: false,
    }))
    await kernel.syncWorkspace(activeWorkspace)
    kernel.openFiltersSession({ draft: true })
    expect(kernel.getCoreSnapshot().filtersSession).toBeNull()
  })
})
