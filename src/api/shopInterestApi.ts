import axiosInstance from "@/api/axiosInstance"
import { SHOP_INTEREST_THANKS_MESSAGE } from "@/lib/operatorShop/shopInterestPresentation"

export type ShopInterestProduct = {
  productKey: string
  title: string
  description: string
  imageUrl: string | null
  interestRegistered: boolean
}

type ShopInterestProductsResponse = {
  success: boolean
  items: ShopInterestProduct[]
}

type RegisterShopInterestResponse = {
  success: boolean
  message: string
}

function mapProduct(raw: {
  productKey: string
  title: string
  description: string
  imageUrl?: string | null
  interestRegistered?: boolean
}): ShopInterestProduct {
  return {
    productKey: raw.productKey,
    title: raw.title,
    description: raw.description,
    imageUrl: raw.imageUrl ?? null,
    interestRegistered: Boolean(raw.interestRegistered),
  }
}

export async function fetchShopInterestProducts(
  locationId: number
): Promise<ShopInterestProduct[]> {
  const response = await axiosInstance.get<ShopInterestProductsResponse>(
    "/shop/interest/products",
    { params: { locationId } }
  )
  return (response.data.items ?? []).map(mapProduct)
}

export async function registerShopInterest(input: {
  productKey: string
  locationId: number
}): Promise<string> {
  const response = await axiosInstance.post<RegisterShopInterestResponse>(
    "/shop/interest",
    {
      productKey: input.productKey,
      locationId: input.locationId,
    }
  )
  return response.data.message ?? SHOP_INTEREST_THANKS_MESSAGE
}

export async function unregisterShopInterest(input: {
  productKey: string
  locationId: number
}): Promise<void> {
  await axiosInstance.delete("/shop/interest", {
    data: {
      productKey: input.productKey,
      locationId: input.locationId,
    },
  })
}
