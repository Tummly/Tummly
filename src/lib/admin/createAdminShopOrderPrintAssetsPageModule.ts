import {
  downloadAdminShopOrderPrintAsset,
  retryAdminShopOrderPrintAsset,
  type AdminShopOrderListItem,
  type AdminShopPrintAsset,
} from "@/api/adminShopOrdersApi"

export type AdminShopOrderPrintAssetRow = AdminShopPrintAsset & {
  canDownload: boolean
  canRetry: boolean
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
}

export type AdminShopOrderPrintAssetsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => AdminShopOrderPrintAssetsSnapshot
  setOrder: (order: AdminShopOrderListItem | null) => void
  download: (qrType: AdminShopPrintAsset["qrType"]) => Promise<boolean>
  retry: (qrType: AdminShopPrintAsset["qrType"]) => Promise<boolean>
}

export const httpAdminShopOrderPrintAssetsAdapters: AdminShopOrderPrintAssetsAdapters =
  {
    download: downloadAdminShopOrderPrintAsset,
    retry: retryAdminShopOrderPrintAsset,
  }

const EMPTY_SNAPSHOT: AdminShopOrderPrintAssetsSnapshot = {
  orderId: null,
  rows: [],
  busyQrType: null,
}

function mapRow(asset: AdminShopPrintAsset): AdminShopOrderPrintAssetRow {
  return {
    ...asset,
    canDownload: asset.status === "Ready",
    canRetry: asset.status === "Failed",
  }
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
        link.download =
          row.fileName ?? `tummly-${orderId}-${qrType.toLowerCase()}.pdf`
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
  }
}
