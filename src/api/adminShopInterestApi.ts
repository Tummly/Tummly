import axiosInstance from "./axiosInstance"

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

type AdminShopInterestListResponse = {
  success: boolean
  items: AdminShopInterestRegistration[]
  countsByProduct: AdminShopInterestProductCount[]
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
