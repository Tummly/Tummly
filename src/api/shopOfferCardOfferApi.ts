import axiosInstance from "@/api/axiosInstance"

export type ShopOfferCardOfferFact = {
  offerId: number | null
  title: string | null
  live: boolean
}

type ShopOfferCardOfferWire = {
  success?: boolean
  offerCardOfferId?: number | null
  offerCardOfferTitle?: string | null
  offerCardOfferLive?: boolean
}

function mapFact(wire: ShopOfferCardOfferWire): ShopOfferCardOfferFact {
  const offerId =
    wire.offerCardOfferId == null ? null : Number(wire.offerCardOfferId)
  return {
    offerId: offerId != null && Number.isFinite(offerId) ? offerId : null,
    title: wire.offerCardOfferTitle ?? null,
    live: wire.offerCardOfferLive === true,
  }
}

export async function fetchShopOfferCardOffer(
  locationId: number
): Promise<ShopOfferCardOfferFact> {
  const response = await axiosInstance.get<ShopOfferCardOfferWire>(
    `/shop/locations/${locationId}/offer-card-offer`
  )
  return mapFact(response.data ?? {})
}

export async function putShopOfferCardOffer(
  locationId: number,
  offerId: number | null
): Promise<ShopOfferCardOfferFact> {
  const response = await axiosInstance.put<ShopOfferCardOfferWire>(
    `/shop/locations/${locationId}/offer-card-offer`,
    { offerId }
  )
  return mapFact(response.data ?? {})
}

/** Mint-accurate Offer Card preview (same compose paint as print PDF). */
export async function fetchShopOfferCardPreview(
  locationId: number,
  headline: string
): Promise<Blob> {
  const response = await axiosInstance.get<Blob>(
    `/shop/locations/${locationId}/offer-card-preview`,
    {
      params: { headline },
      responseType: "blob",
      headers: {
        Accept: "image/png",
      },
      transformRequest: [
        (data, headers) => {
          // Avoid sending JSON Content-Type on a binary GET.
          delete headers["Content-Type"]
          return data
        },
      ],
    }
  )
  const blob = response.data
  if (!(blob instanceof Blob) || blob.size === 0) {
    throw new Error("empty_preview")
  }
  // Axios can deliver error JSON as a Blob when responseType is blob.
  if (blob.type.includes("json") || blob.type.includes("text")) {
    throw new Error("preview_not_image")
  }
  return blob
}
