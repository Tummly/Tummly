import axiosInstance from "./axiosInstance"

export type AdminShopCatalogStatus = "Draft" | "Published" | "Deleted"

export type AdminShopCatalogTag = {
  id: number
  slug: string
  label: string
}

export type AdminShopCatalogImage = {
  id: number
  url: string
  sortOrder: number
}

export type AdminShopCatalogSpecification = {
  label: string
  value: string
}

export type AdminShopCatalogSku = {
  skuId: string
  title: string
  category: string
  description: string
  material: string
  dimensions: string
  qrType: "TableTent" | "WindowSticker" | "OfferCard"
  unitNetPence: number
  minOrderQty: number
  currency: string
  mintOnShopFulfilment: boolean
  status: AdminShopCatalogStatus
  aboutBody: string
  specifications: AdminShopCatalogSpecification[]
  tags: AdminShopCatalogTag[]
  images: AdminShopCatalogImage[]
  publishedAtUtc: string | null
  updatedAtUtc: string
}

export type AdminShopCatalogSkuWrite = {
  skuId?: string
  title?: string
  category?: string
  description?: string
  material?: string
  dimensions?: string
  qrType?: string
  unitNetPence?: number
  minOrderQty?: number
  currency?: string
  mintOnShopFulfilment?: boolean
  aboutBody?: string
  specifications?: AdminShopCatalogSpecification[]
  tagIds?: number[]
}

type ListSkusResponse = { success: boolean; items: AdminShopCatalogSku[] }
type ItemResponse = { success: boolean; item: AdminShopCatalogSku }
type TagListResponse = { success: boolean; items: AdminShopCatalogTag[] }
type TagItemResponse = { success: boolean; item: AdminShopCatalogTag }
type ImageItemResponse = { success: boolean; item: AdminShopCatalogImage }

export async function fetchAdminShopCatalogSkus(
  status?: "Draft" | "Published"
): Promise<AdminShopCatalogSku[]> {
  const response = await axiosInstance.get<ListSkusResponse>(
    "/admin/shop-catalog/skus",
    { params: status ? { status } : undefined }
  )
  return response.data.items
}

export async function createAdminShopCatalogSku(
  body: AdminShopCatalogSkuWrite
): Promise<AdminShopCatalogSku> {
  const response = await axiosInstance.post<ItemResponse>(
    "/admin/shop-catalog/skus",
    body
  )
  return response.data.item
}

export async function updateAdminShopCatalogSku(
  skuId: string,
  body: AdminShopCatalogSkuWrite
): Promise<AdminShopCatalogSku> {
  const response = await axiosInstance.patch<ItemResponse>(
    `/admin/shop-catalog/skus/${encodeURIComponent(skuId)}`,
    body
  )
  return response.data.item
}

export async function publishAdminShopCatalogSku(
  skuId: string
): Promise<AdminShopCatalogSku> {
  const response = await axiosInstance.post<ItemResponse>(
    `/admin/shop-catalog/skus/${encodeURIComponent(skuId)}/publish`
  )
  return response.data.item
}

export async function unpublishAdminShopCatalogSku(
  skuId: string
): Promise<AdminShopCatalogSku> {
  const response = await axiosInstance.post<ItemResponse>(
    `/admin/shop-catalog/skus/${encodeURIComponent(skuId)}/unpublish`
  )
  return response.data.item
}

export async function deleteAdminShopCatalogSku(
  skuId: string
): Promise<AdminShopCatalogSku> {
  const response = await axiosInstance.post<ItemResponse>(
    `/admin/shop-catalog/skus/${encodeURIComponent(skuId)}/delete`
  )
  return response.data.item
}

export async function fetchAdminShopCatalogTags(): Promise<
  AdminShopCatalogTag[]
> {
  const response = await axiosInstance.get<TagListResponse>(
    "/admin/shop-catalog/tags"
  )
  return response.data.items
}

export async function createAdminShopCatalogTag(
  label: string
): Promise<AdminShopCatalogTag> {
  const response = await axiosInstance.post<TagItemResponse>(
    "/admin/shop-catalog/tags",
    { label }
  )
  return response.data.item
}

export async function deleteAdminShopCatalogTag(tagId: number): Promise<void> {
  await axiosInstance.delete(`/admin/shop-catalog/tags/${tagId}`)
}

export async function uploadAdminShopCatalogImage(
  skuId: string,
  file: File
): Promise<AdminShopCatalogImage> {
  const form = new FormData()
  form.append("file", file)
  const response = await axiosInstance.post<ImageItemResponse>(
    `/admin/shop-catalog/skus/${encodeURIComponent(skuId)}/images`,
    form,
    { headers: { "Content-Type": "multipart/form-data" } }
  )
  return response.data.item
}

export async function deleteAdminShopCatalogImage(
  skuId: string,
  imageId: number
): Promise<void> {
  await axiosInstance.delete(
    `/admin/shop-catalog/skus/${encodeURIComponent(skuId)}/images/${imageId}`
  )
}
