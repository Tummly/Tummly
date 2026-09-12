import {
  downloadOperatorPrintMaterial,
  ensureOperatorPrintMaterials,
  retryOperatorPrintMaterial,
} from "@/api/adminApi"
import type {
  AdminPrintMaterialAsset,
  AdminPrintMaterialsLocation,
  AdminPrintMaterialQrType,
  AdminPrintMaterialStatus,
} from "@/types/adminPrintMaterials"

export type OperatorPrintMaterialsAdapters = {
  ensureAndList: (userId: number) => Promise<AdminPrintMaterialsLocation[]>
  download: (input: {
    userId: number
    locationId: number
    qrType: AdminPrintMaterialQrType
  }) => Promise<Blob>
  retry: (input: {
    userId: number
    locationId: number
    qrType: AdminPrintMaterialQrType
  }) => Promise<AdminPrintMaterialAsset>
}

export const httpOperatorPrintMaterialsAdapters: OperatorPrintMaterialsAdapters =
  {
    ensureAndList: ensureOperatorPrintMaterials,
    download: ({ userId, locationId, qrType }) =>
      downloadOperatorPrintMaterial(userId, locationId, qrType),
    retry: ({ userId, locationId, qrType }) =>
      retryOperatorPrintMaterial(userId, locationId, qrType),
  }

export type OperatorPrintMaterialsRow = {
  locationId: number
  locationName: string
  qrType: AdminPrintMaterialQrType
  status: AdminPrintMaterialStatus
  fileName: string | null
  lastError: string | null
  canDownload: boolean
  canRetry: boolean
}

export type OperatorPrintMaterialsSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  rows: OperatorPrintMaterialsRow[]
  errorMessage: string | null
  busyKey: string | null
}

export type OperatorPrintMaterialsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorPrintMaterialsSnapshot
  load: (operatorUserId: number) => Promise<void>
  retryLoad: () => Promise<void>
  download: (
    locationId: number,
    qrType: AdminPrintMaterialQrType
  ) => Promise<boolean>
  retry: (
    locationId: number,
    qrType: AdminPrintMaterialQrType
  ) => Promise<boolean>
}

const EMPTY_SNAPSHOT: OperatorPrintMaterialsSnapshot = {
  loadStatus: "idle",
  rows: [],
  errorMessage: null,
  busyKey: null,
}

function rowKey(locationId: number, qrType: string) {
  return `${locationId}:${qrType}`
}

function mapRows(
  locations: readonly AdminPrintMaterialsLocation[]
): OperatorPrintMaterialsRow[] {
  return locations.flatMap((location) =>
    location.assets.map((asset) => ({
      locationId: location.locationId,
      locationName: location.locationName,
      qrType: asset.qrType,
      status: asset.status,
      fileName: asset.fileName,
      lastError: asset.lastError,
      canDownload: asset.status === "Ready",
      canRetry: asset.status === "Failed",
    }))
  )
}

export function createOperatorPrintMaterialsPageModule(
  adapters: OperatorPrintMaterialsAdapters
): OperatorPrintMaterialsPageModule {
  let snapshot: OperatorPrintMaterialsSnapshot = EMPTY_SNAPSHOT
  let operatorUserId: number | null = null
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const setSnapshot = (next: OperatorPrintMaterialsSnapshot) => {
    snapshot = next
    emit()
  }

  const load = async (userId: number) => {
    operatorUserId = userId
    setSnapshot({
      ...snapshot,
      loadStatus: "loading",
      errorMessage: null,
    })

    try {
      const locations = await adapters.ensureAndList(userId)
      setSnapshot({
        loadStatus: "loaded",
        rows: mapRows(locations),
        errorMessage: null,
        busyKey: null,
      })
    } catch {
      setSnapshot({
        loadStatus: "error",
        rows: [],
        errorMessage: "Could not load print materials.",
        busyKey: null,
      })
    }
  }

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => snapshot,
    load,
    retryLoad: async () => {
      if (operatorUserId == null) {
        return
      }
      await load(operatorUserId)
    },
    download: async (locationId, qrType) => {
      if (operatorUserId == null) {
        return false
      }
      const key = rowKey(locationId, qrType)
      setSnapshot({ ...snapshot, busyKey: key })
      try {
        const blob = await adapters.download({
          userId: operatorUserId,
          locationId,
          qrType,
        })
        const row = snapshot.rows.find(
          (item) => item.locationId === locationId && item.qrType === qrType
        )
        const objectUrl = URL.createObjectURL(blob)
        const link = document.createElement("a")
        link.href = objectUrl
        link.download =
          row?.fileName ??
          `tummly-print-${locationId}-${qrType.toLowerCase()}.pdf`
        link.click()
        URL.revokeObjectURL(objectUrl)
        setSnapshot({ ...snapshot, busyKey: null })
        return true
      } catch {
        setSnapshot({ ...snapshot, busyKey: null })
        return false
      }
    },
    retry: async (locationId, qrType) => {
      if (operatorUserId == null) {
        return false
      }
      const key = rowKey(locationId, qrType)
      setSnapshot({ ...snapshot, busyKey: key })
      try {
        const updated = await adapters.retry({
          userId: operatorUserId,
          locationId,
          qrType,
        })
        setSnapshot({
          ...snapshot,
          busyKey: null,
          rows: snapshot.rows.map((row) =>
            row.locationId === locationId && row.qrType === qrType
              ? {
                  ...row,
                  status: updated.status,
                  fileName: updated.fileName,
                  lastError: updated.lastError,
                  canDownload: updated.status === "Ready",
                  canRetry: updated.status === "Failed",
                }
              : row
          ),
        })
        return updated.status === "Ready"
      } catch {
        setSnapshot({ ...snapshot, busyKey: null })
        return false
      }
    },
  }
}

export function createInMemoryOperatorPrintMaterialsAdapters(initial: {
  locations: AdminPrintMaterialsLocation[]
  failEnsure?: boolean
  failDownload?: boolean
  failRetry?: boolean
  retryRemainsFailed?: boolean
}): OperatorPrintMaterialsAdapters & {
  locations: AdminPrintMaterialsLocation[]
} {
  const state = {
    locations: structuredClone(initial.locations),
  }

  return {
    get locations() {
      return state.locations
    },
    ensureAndList: async () => {
      if (initial.failEnsure) {
        throw new Error("ensure failed")
      }
      return structuredClone(state.locations)
    },
    download: async () => {
      if (initial.failDownload) {
        throw new Error("download failed")
      }
      return new Blob(["%PDF-1.4"], { type: "application/pdf" })
    },
    retry: async ({ locationId, qrType }) => {
      if (initial.failRetry) {
        throw new Error("retry failed")
      }
      for (const location of state.locations) {
        if (location.locationId !== locationId) {
          continue
        }
        for (const asset of location.assets) {
          if (asset.qrType !== qrType) {
            continue
          }
          if (initial.retryRemainsFailed) {
            asset.status = "Failed"
            asset.fileName = null
            asset.lastError = "storage still unavailable"
            return structuredClone(asset)
          }
          asset.status = "Ready"
          asset.fileName = `tummly-${qrType.toLowerCase()}.pdf`
          asset.lastError = null
          return structuredClone(asset)
        }
      }
      throw new Error("asset not found")
    },
  }
}
