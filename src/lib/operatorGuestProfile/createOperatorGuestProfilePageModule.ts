import { isAxiosError } from "axios"

import { mapGuestProfileApiResponseToViewModel } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import type { GuestProfileResponse } from "@/types/dashboard"
import type { OperatorGuestProfileViewModel } from "@/types/operatorGuestProfile"

export type OperatorGuestProfileWorkspaceInput = {
  /** Null when the route `:guestId` is missing or not a positive integer. */
  guestId: number | null
  selectedLocationId: number | null
}

export type OperatorGuestProfilePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "unavailable" | "error"
  viewModel: OperatorGuestProfileViewModel | null
}

export type OperatorGuestProfilePageAdapters = {
  getGuestProfile: (params: {
    guestId: number
    locationId: number
  }) => Promise<GuestProfileResponse>
}

export type OperatorGuestProfilePageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorGuestProfilePageSnapshot
  syncWorkspace: (input: OperatorGuestProfileWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
}

type ModuleState = {
  loadStatus: OperatorGuestProfilePageSnapshot["loadStatus"]
  viewModel: OperatorGuestProfileViewModel | null
  workspace: OperatorGuestProfileWorkspaceInput | null
  fetchedGuestId: number | null
  fetchedLocationId: number | null
  loadGeneration: number
}

function isUnavailableError(error: unknown): boolean {
  return (
    isAxiosError(error) &&
    (error.response?.status === 404 || error.response?.status === 403)
  )
}

function buildSnapshot(state: ModuleState): OperatorGuestProfilePageSnapshot {
  return {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
  }
}

export function createOperatorGuestProfilePageModule(
  adapters: OperatorGuestProfilePageAdapters
): OperatorGuestProfilePageModule {
  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    fetchedGuestId: null,
    fetchedLocationId: null,
    loadGeneration: 0,
  }
  let snapshot = buildSnapshot(state)
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = buildSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const fetchProfile = async () => {
    const workspace = state.workspace
    if (
      workspace == null ||
      workspace.guestId == null ||
      workspace.selectedLocationId == null
    ) {
      return
    }

    const { guestId, selectedLocationId } = workspace
    const generation = state.loadGeneration + 1

    state = {
      ...state,
      loadStatus: "loading",
      loadGeneration: generation,
    }
    publish()

    try {
      const response = await adapters.getGuestProfile({
        guestId,
        locationId: selectedLocationId,
      })

      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: mapGuestProfileApiResponseToViewModel({ response }),
        fetchedGuestId: guestId,
        fetchedLocationId: selectedLocationId,
      }
      publish()
    } catch (error) {
      if (generation !== state.loadGeneration) {
        return
      }

      if (isUnavailableError(error)) {
        state = {
          ...state,
          loadStatus: "unavailable",
          viewModel: null,
          fetchedGuestId: guestId,
          fetchedLocationId: selectedLocationId,
        }
        publish()
        return
      }

      state = {
        ...state,
        loadStatus: "error",
        viewModel: null,
        fetchedGuestId: guestId,
        fetchedLocationId: selectedLocationId,
      }
      publish()
    }
  }

  return {
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot() {
      return snapshot
    },
    async syncWorkspace(input) {
      if (input.guestId == null) {
        state = {
          ...state,
          loadStatus: "unavailable",
          viewModel: null,
          workspace: input,
          fetchedGuestId: null,
          fetchedLocationId: null,
        }
        publish()
        return
      }

      if (input.selectedLocationId == null) {
        state = {
          ...state,
          loadStatus: "idle",
          viewModel: null,
          workspace: input,
          fetchedGuestId: null,
          fetchedLocationId: null,
        }
        publish()
        return
      }

      const samePair =
        state.fetchedGuestId === input.guestId &&
        state.fetchedLocationId === input.selectedLocationId &&
        (state.loadStatus === "loaded" ||
          state.loadStatus === "unavailable" ||
          state.loadStatus === "error")

      state = {
        ...state,
        workspace: input,
      }

      if (samePair) {
        publish()
        return
      }

      await fetchProfile()
    },
    async retryLoad() {
      if (
        state.workspace == null ||
        state.workspace.guestId == null ||
        state.workspace.selectedLocationId == null
      ) {
        return
      }

      state = {
        ...state,
        fetchedGuestId: null,
        fetchedLocationId: null,
      }
      await fetchProfile()
    },
  }
}
