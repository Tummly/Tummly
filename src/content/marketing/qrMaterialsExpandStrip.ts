import { remoteSimpleUrl } from "@/lib/marketingMediaPictures"

const qrFeaturedGuestCardsImage = remoteSimpleUrl(
  "pricing/qr-featured-guest-cards",
)
const qrPeek01Image = remoteSimpleUrl("pricing/qr-peek-01")
const qrPeek02Image = remoteSimpleUrl("pricing/qr-peek-02")
const qrPeek04Image = remoteSimpleUrl("pricing/qr-peek-04")
const qrPeek05Image = remoteSimpleUrl("pricing/qr-peek-05")
const qrPeek06Image = remoteSimpleUrl("pricing/qr-peek-06")

export type MarketingQrExpandSlide = {
  id: string
  image: string
  imageAlt: string
  featured: boolean
}

/**
 * Shared hover-expand QR strip — Figma `5880:19364`.
 * Used on the Pricing QR materials section.
 */
export const MARKETING_QR_EXPAND_STRIP = {
  cardTitle: "QR Guest Cards",
  cardDescription:
    "Put QR Guest Cards into the moments you already hand to guests.",
  /** Default expanded slide is index 2 (featured guest cards). */
  slides: [
    {
      id: "peek-01",
      image: qrPeek01Image,
      imageAlt: "Tummly table tent with QR code on a restaurant table",
      featured: false,
    },
    {
      id: "peek-02",
      image: qrPeek02Image,
      imageAlt: "Tummly QR materials beside takeaway packaging",
      featured: false,
    },
    {
      id: "guest-cards",
      image: qrFeaturedGuestCardsImage,
      imageAlt: "Hand holding a Tummly QR Guest Card offer",
      featured: true,
    },
    {
      id: "peek-04",
      image: qrPeek04Image,
      imageAlt: "Takeaway bag sealed with a Tummly QR sticker",
      featured: false,
    },
    {
      id: "peek-05",
      image: qrPeek05Image,
      imageAlt: "Burger packaging with a Tummly QR Guest Card",
      featured: false,
    },
    {
      id: "peek-06",
      image: qrPeek06Image,
      imageAlt: "Paper bags sealed with green Tummly QR stickers",
      featured: false,
    },
  ] satisfies MarketingQrExpandSlide[],
} as const
