import axiosInstance from "./axiosInstance"

import type {
  AdminShopCatalogPack,
  AdminShopCatalogSpecification,
} from "./adminShopCatalogApi"

export type AdminShopInterestRegistration = {
  id: number
  productKey: string
  productTitle: string
  restaurantId: number
  restaurantName: string
  locationId: number | null
  locationName: string | null
  userId: number
  userFullName: string
  userEmail: string
  createdAtUtc: string
}

export type AdminShopInterestProductCount = {
  productKey: string
  title: string
  count: number
}

export type AdminShopInterestProductImage = {
  id: number
  url: string
  sortOrder: number
}

export type AdminShopInterestProduct = {
  productKey: string
  title: string
  description: string
  material: string
  dimensions: string
  isActive: boolean
  linkedSkuId: string | null
  registrationCount: number
  images: AdminShopInterestProductImage[]
  updatedAtUtc: string
}

export type AdminShopInterestProductWrite = {
  title?: string
  description?: string
  material?: string
  dimensions?: string
}

export type AdminShopInterestPromoteWrite = {
  placementRecipeId: number
  category?: string
  packs?: AdminShopCatalogPack[]
  deliveryNetPence?: number
  supplierName?: string | null
  internalCostNetPence?: number | null
  mintOnShopFulfilment?: boolean
  aboutBody?: string
  specifications?: AdminShopCatalogSpecification[]
  tagIds?: number[]
}

type AdminShopInterestListResponse = {
  success: boolean
  items: AdminShopInterestRegistration[]
  countsByProduct: AdminShopInterestProductCount[]
}

type ProductListResponse = {
  success: boolean
  items: AdminShopInterestProduct[]
}

type ProductItemResponse = {
  success: boolean
  item: AdminShopInterestProduct
}

type ImageItemResponse = {
  success: boolean
  item: AdminShopInterestProductImage
}

export async function fetchAdminShopInterest(): Promise<{
  items: AdminShopInterestRegistration[]
  countsByProduct: AdminShopInterestProductCount[]
}> {
  const response = await axiosInstance.get<AdminShopInterestListResponse>(
    "/admin/shop-interest"
  )
  return {
    items: response.data.items ?? [],
    countsByProduct: response.data.countsByProduct ?? [],
  }
}

export async function fetchAdminShopInterestProducts(): Promise<
  AdminShopInterestProduct[]
> {
  const response = await axiosInstance.get<ProductListResponse>(
    "/admin/shop-interest/products"
  )
  return response.data.items ?? []
}

export async function updateAdminShopInterestProduct(
  productKey: string,
  body: AdminShopInterestProductWrite
): Promise<AdminShopInterestProduct> {
  const response = await axiosInstance.patch<ProductItemResponse>(
    `/admin/shop-interest/products/${encodeURIComponent(productKey)}`,
    body
  )
  return response.data.item
}

export async function uploadAdminShopInterestImage(
  productKey: string,
  file: File
): Promise<AdminShopInterestProductImage> {
  const form = new FormData()
  form.append("file", file)
  const response = await axiosInstance.post<ImageItemResponse>(
    `/admin/shop-interest/products/${encodeURIComponent(productKey)}/images`,
    form,
    { headers: { "Content-Type": "multipart/form-data" } }
  )
  return response.data.item
}

export async function deleteAdminShopInterestImage(
  productKey: string,
  imageId: number
): Promise<void> {
  await axiosInstance.delete(
    `/admin/shop-interest/products/${encodeURIComponent(productKey)}/images/${imageId}`
  )
}

export async function promoteAdminShopInterestProduct(
  productKey: string,
  body: AdminShopInterestPromoteWrite
): Promise<AdminShopInterestProduct> {
  const response = await axiosInstance.post<ProductItemResponse>(
    `/admin/shop-interest/products/${encodeURIComponent(productKey)}/promote`,
    body
  )
  return response.data.item
}

export async function demoteAdminShopInterestProduct(
  productKey: string
): Promise<AdminShopInterestProduct> {
  const response = await axiosInstance.post<ProductItemResponse>(
    `/admin/shop-interest/products/${encodeURIComponent(productKey)}/demote`
  )
  return response.data.item
}

export async function demoteAdminShopInterestBySku(
  skuId: string
): Promise<AdminShopInterestProduct> {
  const response = await axiosInstance.post<ProductItemResponse>(
    `/admin/shop-interest/demote-by-sku/${encodeURIComponent(skuId)}`
  )
  return response.data.item
}
