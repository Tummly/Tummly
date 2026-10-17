import {
  approveAdminShopOrderPrintAsset,
  downloadAdminShopOrderPrintAsset,
  previewAdminShopOrderPrintAsset,
  retryAdminShopOrderPrintAsset,
  type AdminShopOrderListItem,
  type AdminShopPrintAsset,
  type AdminShopPrintAssetPreview,
} from "@/api/adminShopOrdersApi"

export type AdminShopOrderPrintAssetRow = AdminShopPrintAsset & {
  canDownload: boolean
  canRetry: boolean
  canApprove: boolean
}

export type AdminShopOrderPrintAssetsSnapshot = {
  orderId: string | null
  rows: AdminShopOrderPrintAssetRow[]
  busyQrType: AdminShopPrintAsset["qrType"] | null
}

export type AdminShopOrderPrintAssetsAdapters = {
  download: (
    orderId: string,
    qrType: AdminShopPrintAsset["qrType"]
  ) => Promise<Blob>
  retry: (
    orderId: string,
    qrType: AdminShopPrintAsset["qrType"]
  ) => Promise<AdminShopPrintAsset>
  approve: (
    orderId: string,
    qrType: AdminShopPrintAsset["qrType"]
  ) => Promise<AdminShopPrintAsset>
  preview?: (
    orderId: string,
    qrType: AdminShopPrintAsset["qrType"]
  ) => Promise<AdminShopPrintAssetPreview>
}

export type AdminShopOrderPrintAssetsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => AdminShopOrderPrintAssetsSnapshot
  setOrder: (order: AdminShopOrderListItem | null) => void
  download: (qrType: AdminShopPrintAsset["qrType"]) => Promise<boolean>
  retry: (qrType: AdminShopPrintAsset["qrType"]) => Promise<boolean>
  approve: (qrType: AdminShopPrintAsset["qrType"]) => Promise<boolean>
  preview: (
    qrType: AdminShopPrintAsset["qrType"]
  ) => Promise<AdminShopPrintAssetPreview | null>
}

export const httpAdminShopOrderPrintAssetsAdapters: AdminShopOrderPrintAssetsAdapters =
  {
    download: downloadAdminShopOrderPrintAsset,
    retry: retryAdminShopOrderPrintAsset,
    approve: approveAdminShopOrderPrintAsset,
    preview: previewAdminShopOrderPrintAsset,
  }

const EMPTY_SNAPSHOT: AdminShopOrderPrintAssetsSnapshot = {
  orderId: null,
  rows: [],
  busyQrType: null,
}

function mapRow(asset: AdminShopPrintAsset): AdminShopOrderPrintAssetRow {
  const ready = asset.status === "Ready"
  const approved = asset.approved === true
  return {
    ...asset,
    canDownload: ready && approved,
    canRetry: asset.status === "Failed",
    canApprove: ready && !approved,
  }
}

function defaultDownloadName(
  orderId: string,
  qrType: AdminShopPrintAsset["qrType"],
  fileName: string | null
): string {
  if (fileName && fileName.trim().length > 0) {
    return fileName
  }
  return `tummly-${orderId}-${qrType.toLowerCase()}.pdf`
}

export function createAdminShopOrderPrintAssetsPageModule(
  adapters: AdminShopOrderPrintAssetsAdapters
): AdminShopOrderPrintAssetsPageModule {
  let snapshot = EMPTY_SNAPSHOT
  const listeners = new Set<() => void>()

  const publish = (next: AdminShopOrderPrintAssetsSnapshot) => {
    snapshot = next
    for (const listener of listeners) {
      listener()
    }
  }

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    getSnapshot: () => snapshot,
    setOrder: (order) => {
      publish(
        order == null
          ? EMPTY_SNAPSHOT
          : {
              orderId: order.id,
              rows: (order.printAssets ?? []).map(mapRow),
              busyQrType: null,
            }
      )
    },
    download: async (qrType) => {
      const row = snapshot.rows.find((item) => item.qrType === qrType)
      if (snapshot.orderId == null || !row?.canDownload) {
        return false
      }

      const orderId = snapshot.orderId
      publish({ ...snapshot, busyQrType: qrType })
      try {
        const blob = await adapters.download(orderId, qrType)
        const objectUrl = URL.createObjectURL(blob)
        const link = document.createElement("a")
        link.href = objectUrl
        link.download = defaultDownloadName(orderId, qrType, row.fileName)
        link.click()
        URL.revokeObjectURL(objectUrl)
        publish({ ...snapshot, busyQrType: null })
        return true
      } catch {
        publish({ ...snapshot, busyQrType: null })
        return false
      }
    },
    retry: async (qrType) => {
      const row = snapshot.rows.find((item) => item.qrType === qrType)
      if (snapshot.orderId == null || !row?.canRetry) {
        return false
      }

      const orderId = snapshot.orderId
      publish({ ...snapshot, busyQrType: qrType })
      try {
        const updated = await adapters.retry(orderId, qrType)
        publish({
          ...snapshot,
          busyQrType: null,
          rows: snapshot.rows.map((item) =>
            item.qrType === qrType ? mapRow(updated) : item
          ),
        })
        return updated.status === "Ready"
      } catch {
        publish({ ...snapshot, busyQrType: null })
        return false
      }
    },
    approve: async (qrType) => {
      const row = snapshot.rows.find((item) => item.qrType === qrType)
      if (snapshot.orderId == null || !row?.canApprove) {
        return false
      }

      const orderId = snapshot.orderId
      publish({ ...snapshot, busyQrType: qrType })
      try {
        const updated = await adapters.approve(orderId, qrType)
        publish({
          ...snapshot,
          busyQrType: null,
          rows: snapshot.rows.map((item) =>
            item.qrType === qrType ? mapRow(updated) : item
          ),
        })
        return updated.approved === true
      } catch {
        publish({ ...snapshot, busyQrType: null })
        return false
      }
    },
    preview: async (qrType) => {
      if (snapshot.orderId == null || adapters.preview == null) {
        return null
      }
      try {
        return await adapters.preview(snapshot.orderId, qrType)
      } catch {
        return null
      }
    },
  }
}
