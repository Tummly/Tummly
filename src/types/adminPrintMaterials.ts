export type AdminPrintMaterialQrType =
  | "TableTent"
  | "WindowSticker"
  | "OfferCard"

export type AdminPrintMaterialStatus = "Preparing" | "Ready" | "Failed"

export type AdminPrintMaterialAsset = {
  qrType: AdminPrintMaterialQrType
  status: AdminPrintMaterialStatus
  fileName: string | null
  lastError: string | null
}

export type AdminPrintMaterialsLocation = {
  locationId: number
  locationName: string
  assets: AdminPrintMaterialAsset[]
}
