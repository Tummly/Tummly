import axiosInstance from "@/api/axiosInstance"
import { SHOP_INTEREST_THANKS_MESSAGE } from "@/lib/operatorShop/shopInterestPresentation"

export type ShopInterestProduct = {
  productKey: string
  title: string
  description: string
  imageUrl: string | null
}

type ShopInterestProductsResponse = {
  success: boolean
  items: ShopInterestProduct[]
}

type RegisterShopInterestResponse = {
  success: boolean
  message: string
}

export async function fetchShopInterestProducts(
  locationId: number
): Promise<ShopInterestProduct[]> {
  const response = await axiosInstance.get<ShopInterestProductsResponse>(
    "/shop/interest/products",
    { params: { locationId } }
  )
  return response.data.items ?? []
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
